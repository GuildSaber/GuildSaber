using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.Auth.Sessions;
using GuildSaber.Api.Features.Auth.Settings;
using GuildSaber.Common.Services.BeatLeader;
using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.Mappers.BeatLeader;
using GuildSaber.Database.Models.Server.Auth;
using GuildSaber.Database.Models.Server.Players;
using GuildSaber.Database.Models.StrongTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MyCSharp.HttpUserAgentParser.AspNetCore;

namespace GuildSaber.Api.Features.Auth;

public class AuthService(
    JwtService jwtService,
    IOptions<SessionSettings> sessionSettings,
    IOptions<ManagerSettings> managerSettings,
    IHttpUserAgentParserAccessor userAgentParser,
    BeatLeaderApi beatLeaderApi,
    ServerDbContext dbContext,
    TimeProvider timeProvider)
{
    public async Task<Maybe<PlayerId>> GetPlayerIdAsync(BeatLeaderId beatLeaderId)
        => await dbContext.Players
                .Where(p => p.LinkedAccounts.BeatLeaderId == beatLeaderId)
                .Select(p => p.Id)
                .FirstOrDefaultAsync() switch
            {
                { Value: 0 } => None,
                var id => From(id)
            };

    public async Task<Maybe<PlayerId>> GetPlayerIdAsync(DiscordId discordId)
        => await dbContext.Players
                .Where(p => p.LinkedAccounts.DiscordId == discordId)
                .Select(p => p.Id)
                .FirstOrDefaultAsync() switch
            {
                { Value: 0 } => None,
                var id => From(id)
            };

    public async Task<bool> LinkDiscordIdAsync(PlayerId playerId, DiscordId discordId)
    {
        var player = await dbContext.Players.AsTracking()
            .Where(x => x.Id == playerId)
            .FirstOrDefaultAsync();
        if (player is null) return false;

        player.LinkedAccounts = player.LinkedAccounts with { DiscordId = discordId };
        return await dbContext.SaveChangesAsync() > 0;
    }

    private async Task<int> GetValidSessionCountAsync(PlayerId playerId, DateTimeOffset currentTime)
        => await dbContext.Sessions
            .CountAsync(s => s.PlayerId == playerId && s.IsValid && s.ExpiresAt > currentTime);

    public async Task<Result<string, SessionCreationError>> CreateSession(PlayerId playerId, HttpContext httpContext)
    {
        var userAgent = userAgentParser.Get(httpContext);
        if (userAgent is null)
            return new MissingUserAgent();

        var sessionCount = await GetValidSessionCountAsync(playerId, timeProvider.GetUtcNow());
        var settings = sessionSettings.Value;

        if (sessionCount >= settings.MaxSessionCount)
            return new TooManyOpenSession(sessionCount, settings.MaxSessionCount);

        var token = jwtService.CreateToken(settings.ExpireAfter);
        var session = new Session
        {
            SessionId = token.Identifier,
            PlayerId = playerId,
            IssuedAt = token.IssuedAt,
            ExpiresAt = token.ExpireAt,
            Browser = userAgent.Value.Name ?? "Unknown",
            BrowserVersion = userAgent.Value.Version ?? "Unknown",
            Platform = userAgent.Value.Platform?.Name ?? "Unknown",
            IsValid = true
        };

        _ = await dbContext.AddAndSaveAsync(session);

        return token.Token;
    }

    public async Task<Result<PlayerId>> CreateUserAsync(BeatLeaderId beatleaderId)
        => await beatLeaderApi.GetPlayerProfileWithStatsAsync(beatleaderId)
            .Bind(blPlayer => blPlayer == null
                ? Failure<Player>("Player not found on BeatLeader.")
                : Success(new Player
                {
                    Info = new PlayerInfo
                    {
                        Username = blPlayer.Name,
                        AvatarUrl = blPlayer.Avatar,
                        Country = blPlayer.Country
                    },
                    HardwareInfo = new PlayerHardwareInfo
                    {
                        HMD = blPlayer.ScoreStats.TopHMD.Map(),
                        Platform = PlatformMappers.Map(blPlayer.Platform)
                    },
                    LinkedAccounts = new PlayerLinkedAccounts(beatleaderId, null, null),
                    SubscriptionInfo = new PlayerSubscriptionInfo(PlayerSubscriptionInfo.ESubscriptionTier.None),
                    IsManager = managerSettings.Value.SteamIds.Contains(blPlayer.Id)
                }))
            .Map(static (player, dbContext) => dbContext
                .AddAndSaveAsync(player, x => x.Id), dbContext);
}

public abstract record SessionCreationError;
public record TooManyOpenSession(int CurrentCount, int MaxCount) : SessionCreationError;
public record MissingUserAgent : SessionCreationError;
public record AccountLocked : SessionCreationError;