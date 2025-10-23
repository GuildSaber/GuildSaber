using System.Drawing;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.Guilds.Members;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Members;
using GuildSaber.Database.Models.Server.Players;
using GuildSaber.Database.Models.StrongTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GuildSaber.Api.Features.Guilds;

public class GuildService(ServerDbContext dbContext, TimeProvider timeProvider, IOptions<GuildSettings> guildSettings)
{
    public abstract record CreateResponse
    {
        public record Success(Guild Guild) : CreateResponse;
        public record PlayerNotFound : CreateResponse;
        public record ValidationFailure(IEnumerable<KeyValuePair<string, string[]>> Errors) : CreateResponse;
        public record RequirementsFailure(IEnumerable<KeyValuePair<string, string[]>> Errors) : CreateResponse;
        public record TooManyGuildsAsLeader(int CurrentCount, int MaxCount) : CreateResponse;
    }

    /// <summary>
    /// Processes a player's request to create a new guild
    /// </summary>
    /// <param name="playerId">The unique identifier of the player requesting to create the guild</param>
    /// <param name="request">The guild creation request containing name, description, and color</param>
    /// <returns>
    ///     <para><see cref="CreateResponse.Success" /> when guild is created successfully</para>
    ///     <para><see cref="CreateResponse.PlayerNotFound" /> when the specified player doesn't exist</para>
    ///     <para><see cref="CreateResponse.ValidationFailure" /> when input validation fails</para>
    ///     <para><see cref="CreateResponse.RequirementsFailure" /> when player fails to meet guild creation requirements</para>
    ///     <para>
    ///     <see cref="CreateResponse.TooManyGuildsAsLeader" /> when player has reached the maximum number of guilds they can
    ///     lead
    ///     </para>
    /// </returns>
    /// <remarks>
    /// The method validates that the player meets subscription tier requirements before allowing guild creation.
    /// Guild names must be between 5-50 characters and descriptions must be valid.
    /// All newly created guilds start with an Unverified status.
    /// </remarks>
    public async Task<CreateResponse> CreateGuildAsync(PlayerId playerId, GuildRequests.CreateGuild request)
        => await GetPlayerSubscriptionInfo(playerId)
            .ToResult(CreateResponse () => new CreateResponse.PlayerNotFound())
            .Check(async _ => await ValidateUserGuildCreationLimit(playerId)
                .MapError(CreateResponse (x) => new CreateResponse.TooManyGuildsAsLeader(x.current, x.max)))
            .Check(subInfo => ValidateCreationRequirements(subInfo, guildSettings.Value.Creation)
                .MapError(CreateResponse (errors) => new CreateResponse.RequirementsFailure(errors)))
            .Bind(_ => MakeGuildAndValidate(request)
                .MapError(CreateResponse (errors) => new CreateResponse.ValidationFailure(errors)))
            .Map(static (guild, state) => state.dbContext.Database.CreateExecutionStrategy()
                    .ExecuteInTransactionAsync((guild, state.playerId, state.dbContext, state.timeProvider),
                        operation: static async (state, _) =>
                        {
                            var inserted = await state.dbContext.AddAndSaveAsync(state.guild);
                            var currTime = state.timeProvider.GetUtcNow();

                            await state.dbContext.AddAndSaveAsync(new Member
                            {
                                GuildId = state.guild.Id,
                                PlayerId = state.playerId,
                                CreatedAt = currTime,
                                EditedAt = currTime,
                                Permissions = EPermission.GuildLeader,
                                JoinState = Member.EJoinState.None,
                                Priority = await MemberService.GetNextPriority(state.dbContext, state.playerId)
                            });

                            return inserted;
                        }, verifySucceeded: (inserted, _) => Task.FromResult(inserted.guild.Id != 0)),
                (dbContext, playerId, timeProvider))
            .Match(guild => new CreateResponse.Success(guild), err => err);

    /// <summary>
    /// Validates the guild creation request and constructs a new Guild object
    /// </summary>
    private static Result<Guild, List<KeyValuePair<string, string[]>>> MakeGuildAndValidate(
        GuildRequests.CreateGuild request)
    {
        var nameResult = Name_5_50.TryCreate(request.Info.Name);
        var descriptionResult = Description.TryCreate(request.Info.Description);

        List<KeyValuePair<string, string[]>> validationErrors = [];
        if (!nameResult.TryGetValue(out var name))
            validationErrors.Add(new KeyValuePair<string, string[]>(
                nameof(GuildRequests.CreateGuildInfo.Name), [nameResult.Error]));

        if (!Name_2_6.TryCreate(request.Info.SmallName, "Small name").TryGetValue(out var smallName, out var nameError))
            validationErrors.Add(new KeyValuePair<string, string[]>(
                nameof(GuildRequests.CreateGuildInfo.SmallName), [nameError]));

        if (!descriptionResult.TryGetValue(out var description, out var descriptionError))
            validationErrors.Add(new KeyValuePair<string, string[]>(
                nameof(GuildRequests.CreateGuildInfo.Description), [descriptionError]));

        if (validationErrors.Count > 0)
            return Failure<Guild, List<KeyValuePair<string, string[]>>>(validationErrors);

        return Success<Guild, List<KeyValuePair<string, string[]>>>(new Guild
        {
            Info = new GuildInfo
            {
                Name = name,
                SmallName = smallName,
                Description = description,
                Color = Color.FromArgb(request.Info.Color),
                CreatedAt = DateTimeOffset.UtcNow
            },
            Requirements = request.Requirements.Map(),
            Status = Guild.EGuildStatus.Unverified
        });
    }

    /// <summary>
    /// Retrieves player's subscription information from the database
    /// </summary>
    /// <param name="playerId">The unique identifier of the player</param>
    /// <returns>
    /// The player's subscription information if found, None if the player doesn't exist
    /// </returns>
    private async Task<Maybe<PlayerSubscriptionInfo>> GetPlayerSubscriptionInfo(
        PlayerId playerId)
        => await dbContext.Players.Where(p => p.Id == playerId)
                .Select(p => p.SubscriptionInfo)
                .Cast<PlayerSubscriptionInfo?>()
                .FirstOrDefaultAsync() switch
            {
                null => None,
                var x => From(x.Value)
            };

    private async Task<UnitResult<(int current, int max)>> ValidateUserGuildCreationLimit(PlayerId playerId)
        => await GetUserCreatedGuildCountAsync(playerId) switch
        {
            var count when count >= guildSettings.Value.Creation.MaxGuildCountPerUser
                => Failure((count, guildSettings.Value.Creation.MaxGuildCountPerUser)),
            _ => UnitResult.Success<(int, int)>()
        };

    /// <remarks>
    /// GuildLeaded guilds are abstracted as guilds "created" by a user.
    /// (Leading a guild either implies creation, or heavy involvement, which would be similar to owning it)
    /// </remarks>
    private async Task<int> GetUserCreatedGuildCountAsync(PlayerId playerId)
        => await dbContext.Members.CountAsync(x => x.PlayerId == playerId && x.Permissions == EPermission.GuildLeader);

    /// <summary>
    /// Validates if a player meets guild creation requirements based on their subscription tier
    /// </summary>
    /// <param name="subscriptionInfo">The player's subscription information</param>
    /// <param name="settings">The guild creation settings to check against</param>
    /// <returns>
    /// Success if requirements are met, or Failure with specific validation errors when requirements are not met
    /// </returns>
    /// <remarks>
    /// Currently checks if the player's subscription tier meets the minimum required tier for guild creation
    /// </remarks>
    private static UnitResult<IEnumerable<KeyValuePair<string, string[]>>>
        ValidateCreationRequirements(PlayerSubscriptionInfo subscriptionInfo, GuildCreationSettings settings)
        => subscriptionInfo switch
        {
            { Tier: var tier } when (int)tier < (int)settings.RequiredSubscriptionTier
                => Failure<IEnumerable<KeyValuePair<string, string[]>>>([
                        new KeyValuePair<string, string[]>(
                            "SubscriptionTier",
                            [
                                $"You must have a subscription tier of at least {settings.RequiredSubscriptionTier} to create a guild."
                            ]
                        )
                    ]
                ),
            _ => UnitResult.Success<IEnumerable<KeyValuePair<string, string[]>>>()
        };
}