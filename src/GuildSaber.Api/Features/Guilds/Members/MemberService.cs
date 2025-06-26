using CSharpFunctionalExtensions;
using GuildSaber.Common.Services.BeatLeader;
using GuildSaber.Common.Services.BeatLeader.Models;
using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Members;
using Microsoft.EntityFrameworkCore;
using static GuildSaber.Database.Models.Server.Guilds.GuildRequirementsExtensions.GuildRequirement;
using static GuildSaber.Api.Features.Guilds.Members.MemberService.JoinResponse;

namespace GuildSaber.Api.Features.Guilds.Members;

public class MemberService(ServerDbContext dbContext, BeatLeaderApi beatLeaderApi, TimeProvider timeProvider)
{
    public abstract record JoinResponse
    {
        public record Success(Member Member) : JoinResponse;
        public record Requested(Member Member) : JoinResponse;
        public record AlreadyMember(Member Member) : JoinResponse;
        public record PlayerNotFound : JoinResponse;
        public record GuildNotFound : JoinResponse;
        public record BeatLeaderProfileNotFound(BeatLeaderId BeatLeaderId, string Error) : JoinResponse;
        public record RequirementsFailure(IEnumerable<KeyValuePair<string, string[]>> Errors) : JoinResponse;
    }

    /// <summary>
    /// Processes a player's request to join a specific guild
    /// </summary>
    /// <param name="guildId">The unique identifier of the guild to join</param>
    /// <param name="playerId">The unique identifier of the player requesting to join</param>
    /// <returns>
    ///     <para><see cref="JoinResponse.Success" /> when player joins the guild successfully</para>
    ///     <para><see cref="JoinResponse.Requested" /> when player's join request is submitted for guild approval</para>
    ///     <para><see cref="JoinResponse.AlreadyMember" /> when player is already a member of the guild</para>
    ///     <para><see cref="JoinResponse.GuildNotFound" /> when the specified guild doesn't exist</para>
    ///     <para><see cref="JoinResponse.PlayerNotFound" /> when the specified player doesn't exist</para>
    ///     <para><see cref="JoinResponse.BeatLeaderProfileNotFound" /> when player's BeatLeader profile cannot be found</para>
    ///     <para><see cref="JoinResponse.RequirementsFailure" /> when player fails to meet guild requirements</para>
    /// </returns>
    /// <remarks>
    /// The method validates that the player meets all guild requirements before allowing them to join.
    /// If the guild has submission requirement flag enabled, the player's join request will need approval.
    /// </remarks>
    public async Task<JoinResponse> JoinGuildAsync(GuildId guildId, PlayerId playerId)
        => await GetMemberAsFailure(dbContext, guildId, playerId)
            .MapError(JoinResponse (error) => new AlreadyMember(error))
            .Bind(() => ValidateRequirementsAndProfile(guildId, playerId))
            .Check(x => GetRequirementsErrorAsFailure(x.requirements, x.blProfile)
                .MapError(JoinResponse (errors) => new RequirementsFailure(errors)))
            .Map(static async (x, context) => new Member
            {
                PlayerId = context.playerId,
                GuildId = context.guildId,
                CreatedAt = context.timeProvider.GetUtcNow(),
                EditedAt = context.timeProvider.GetUtcNow(),
                JoinState = x.requirements.RequireSubmission
                    ? Member.EJoinState.Requested
                    : Member.EJoinState.Joined,
                Permissions = EPermission.None,
                Priority = await GetNextPriority(context.dbContext, context.playerId)
            }, (playerId, guildId, timeProvider, dbContext))
            .Map(async static (member, dbContext) => await dbContext
                .AddAndSaveAsync(member), dbContext)
            .Match(
                member => member.JoinState == Member.EJoinState.Requested
                    ? new Requested(member)
                    : new Success(member),
                err => err
            );

    /// <summary>
    /// Validates guild requirements and retrieves player's BeatLeader profile
    /// </summary>
    /// <param name="guildId">The unique identifier of the guild</param>
    /// <param name="playerId">The unique identifier of the player</param>
    /// <returns>
    /// A result containing the guild requirements and player's BeatLeader profile if successful,
    /// or a failure with appropriate error response
    /// </returns>
    private async Task<Result<(GuildRequirements requirements, PlayerResponseFullWithStats blProfile), JoinResponse>>
        ValidateRequirementsAndProfile(GuildId guildId, PlayerId playerId)
        => await (from guildReq in GetGuildRequirements(dbContext, guildId)
                      .ToResult(() => (JoinResponse)new GuildNotFound())
                  from beatLeaderId in GetBeatLeaderId(dbContext, playerId)
                      .ToResult(() => (JoinResponse)new PlayerNotFound())
                  from blProfile in beatLeaderApi.GetPlayerProfileWithStats(beatLeaderId)
                      .MapError(err => (JoinResponse)new BeatLeaderProfileNotFound(beatLeaderId, err))
                  select (guildReq, blProfile));

    /// <summary>
    /// Validates if a player meets all guild requirements based on their BeatLeader profile
    /// </summary>
    /// <param name="requirements">The guild requirements configuration to check against</param>
    /// <param name="profile">The player's BeatLeader profile with stats</param>
    /// <returns>
    /// Success if all requirements are met, or Failure with specific validation errors when requirements are not met
    /// </returns>
    private static UnitResult<IEnumerable<KeyValuePair<string, string[]>>> GetRequirementsErrorAsFailure(
        GuildRequirements requirements, PlayerResponseFullWithStats profile) => requirements.Collect()
            .Select<GuildRequirementsExtensions.GuildRequirement, KeyValuePair<string, string[]>?>(reqEnum
                => reqEnum switch
                {
                    RequireSubmission => null, // Technically not an error
                    MinRank when profile.Rank < requirements.MinRank => new KeyValuePair<string, string[]>(
                        nameof(MinRank),
                        [
                            $"Your rank ({profile.Rank}) is below the minimum required rank ({requirements.MinRank})."
                        ]),
                    MaxRank when profile.Rank > requirements.MaxRank => new KeyValuePair<string, string[]>(
                        nameof(MaxRank),
                        [
                            $"Your rank ({profile.Rank}) is above the maximum allowed rank ({requirements.MaxRank})."
                        ]),
                    MinPP when profile.Pp < requirements.MinPP => new KeyValuePair<string, string[]>(nameof(MinPP),
                        [$"Your PP ({profile.Pp}) is below the minimum required PP ({requirements.MinPP})."]),
                    MaxPP when profile.Pp > requirements.MaxPP => new KeyValuePair<string, string[]>(nameof(MaxPP),
                        [$"Your PP ({profile.Pp}) is above the maximum allowed PP ({requirements.MaxPP})."]),
                    AccountAgeUnix when profile.ScoreStats.FirstScoreTime < requirements.AccountAgeUnix
                        => new KeyValuePair<string, string[]>(nameof(AccountAgeUnix),
                        [
                            $"Your account age ({profile.ScoreStats.FirstScoreTime}) is below the minimum required account age ({requirements.AccountAgeUnix})."
                        ]),
                    _ => null
                })
            .Where(x => x is not null)
            .Select(errors => errors!.Value).ToArray() switch
        {
            var errors when errors.Length != 0 => Failure<IEnumerable<KeyValuePair<string, string[]>>>(errors),
            _ => UnitResult.Success<IEnumerable<KeyValuePair<string, string[]>>>()
        };

    /// <summary>
    /// Retrieves the requirements configuration for a specific guild
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="guildId">The unique identifier of the guild</param>
    /// <returns>
    /// Guild requirements if found, None if the guild does not exist
    /// </returns>
    private static async Task<Maybe<GuildRequirements>> GetGuildRequirements(
        ServerDbContext dbContext, GuildId guildId)
    {
        var res = await dbContext.Guilds
            .Where(x => x.Id == guildId)
            .Select(x => new { x.Requirements })
            .FirstOrDefaultAsync();

        return res is null ? Maybe<GuildRequirements>.None : From(res.Requirements);
    }

    /// <summary>
    /// Retrieves a player's linked BeatLeader ID
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="playerId">The unique identifier of the player</param>
    /// <returns>
    /// The player's BeatLeader ID if linked, None if the player doesn't exist or has no linked BeatLeader account
    /// </returns>
    private static async Task<Maybe<BeatLeaderId>> GetBeatLeaderId(
        ServerDbContext dbContext, PlayerId playerId)
        => await dbContext.Players.Where(x => x.Id == playerId)
                .Select(x => x.LinkedAccounts.BeatLeaderId)
                .Cast<BeatLeaderId?>()
                .FirstOrDefaultAsync() switch
            {
                null => Maybe<BeatLeaderId>.None,
                var beatLeaderId => From(beatLeaderId.Value)
            };

    /// <summary>
    /// Checks if a player is already a member of a specific guild
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="guildId">The unique identifier of the guild</param>
    /// <param name="playerId">The unique identifier of the player</param>
    /// <returns>
    /// Success if the player is not a member, Failure with existing member data if already a member
    /// </returns>
    private static async Task<UnitResult<Member>> GetMemberAsFailure(
        ServerDbContext dbContext, GuildId guildId, PlayerId playerId)
        => await dbContext.Members.FirstOrDefaultAsync(x => x.GuildId == guildId && x.PlayerId == playerId) switch
        {
            null => UnitResult.Success<Member>(),
            var member => Failure(member)
        };

    /// <summary>
    /// Calculates the next available priority value for a player joining a new guild
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="playerId">The unique identifier of the player</param>
    /// <returns>
    /// The next available priority value for the player
    /// </returns>
    /// <remarks>
    /// There is indeed more optimized way to do this, but if it works, and don't cause problem, don't touch it.
    /// </remarks>
    public static async Task<int> GetNextPriority(ServerDbContext dbContext, PlayerId playerId)
        => (await dbContext.Members
                .Where(x => x.PlayerId == playerId)
                .Select(x => x.Priority)
                .ToListAsync())
            .Aggregate(1, (current, session) => session > current ? current : current + 1);
}