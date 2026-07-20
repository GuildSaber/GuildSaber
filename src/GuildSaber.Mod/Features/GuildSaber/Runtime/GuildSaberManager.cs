using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GuildSaber.Api.Features.Guilds.Http;
using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using GuildSaber.Common.StrongTypes;
using GuildSaber.CSharpClient;
using Zenject;

namespace GuildSaber.Mod.Features.GuildSaber.Runtime;

[SuppressMessage("ReSharper", "AsyncVoidMethod")]
public class GuildSaberManager(
    GuildSaberClient client,
    GuildSaberSession session,
    GuildSaberConfig config,
    IPlatformUserModel platformLeaderboardsModel,
    Logger logger
) : IInitializable
{
    public bool Initialized;
    public bool Errored;

    public async void Initialize()
    {
        logger.Info("Initializing GuildSaberManager...");

        var userInfo = await platformLeaderboardsModel.GetUserInfo(CancellationToken.None);
        if (!BeatLeaderId.TryParse(userInfo.platformUserId).TryGetValue(out var beatLeaderId, out var error))
        {
            logger.Warn($"Failed to parse BeatLeaderId: {error}. " +
                        "Invoking OnPlayerIdFetched with null and terminating Initialize.");
            Errored = true;
            OnInitializationError("Failed to parse BeatLeaderId from user information.");
            return;
        }

        logger.Info($"Fetched BeatLeaderId: {beatLeaderId} of kind {beatLeaderId.Kind}.");

        if (!(await client.Players.LookupPlayerIdByBeatLeaderIdAsync(beatLeaderId))
            .TryGetValue(out var playerId, out error))
        {
            logger.Error($"Failed to fetch PlayerId: {error}. " +
                         "Invoking OnPlayerIdFetched with null and terminating Initialize.");
            Errored = true;
            OnInitializationError("Failed to fetch PlayerId from GuildSaber.");
            return;
        }

        if (playerId == null)
        {
            logger.Error("PlayerId is null, user does not have an account on GuildSaber. " +
                         "Invoking OnPlayerIdFetched with null and terminating Initialize.");
            Errored = true;
            OnInitializationError("User does not have an account on GuildSaber.");
            return;
        }

        var extendedPlayerIdResult = await client.Players.GetExtendedByIdAsync(playerId.Value);
        if (!extendedPlayerIdResult.TryGetValue(out var extendedPlayer, out error))
        {
            logger.Error($"Failed to fetch extended player: {error}");
            logger.Error("Terminating");
            Errored = true;
            OnInitializationError.Invoke(error);
            return;
        }

        if (extendedPlayer == null)
        {
            logger.Error("Extended player response is null.");
            Errored = true;
            OnInitializationError.Invoke("Failed to fetch extended player from GuildSaber.");
            return;
        }

        session.SetPlayer(extendedPlayer);

        var guildExtendeds = await Task.WhenAll(extendedPlayer.Members.Select(async member =>
        {
            var guildResponse = await client.Guilds.GetExtendedByIdAsync(new GuildId(member.GuildId));
            if (guildResponse.TryGetValue(out var guild, out var guildError)) return guild;

            logger.Error($"Failed to fetch guild: {guildError}. ");
            Errored = true;
            OnInitializationError("Failed to fetch PlayerId from GuildSaber.");
            return null;
        }));

        session.SetGuilds(guildExtendeds.OfType<GuildResponses.GuildExtended>());

        if (!session.HasAvailableGuilds)
        {
            logger.Warn("Player is not a member of any guild. " +
                        "Invoking OnPlayerIdFetched with null and terminating Initialize.");
            OnNoGuildError();
            return;
        }

        // Make sure the config selected guild and context exists, if not select the first one.
        if (!session.TryGetGuild(config.GuildId, out var selectedGuild))
        {
            var firstGuildExtended = session.GetAvailableGuilds().First();
            config.GuildId = firstGuildExtended.Guild.Id;
            config.ContextId = firstGuildExtended.Contexts[0].Id;
        }
        else
        {
            if (selectedGuild.Contexts.All(x => x.Id != config.ContextId))
                config.ContextId = selectedGuild.Contexts[0].Id;
        }

        SelectGuild(config.GuildId, config.ContextId);
    }

    public event Action<string> OnInitializationError = _ => { };
    public event Action OnNoGuildError = () => { };
    public event Action OnGuildSelectionStarted = () => { };
    public event Action<ContextId> OnMemberStatsRefreshed = _ => { };

    public void SetGuild(GuildResponses.GuildExtended guild)
    {
        config.GuildId = guild.Guild.Id;
        config.ContextId = guild.Contexts[0].Id;

        SelectGuild(guild.Guild.Id, guild.Contexts[0].Id);
    }

    public async void SelectGuild(GuildId guildId, ContextId contextId)
    {
        OnGuildSelectionStarted.Invoke();

        if (!session.HasLocalPlayer)
        {
            logger.Info("Local player is not loaded.");
            return;
        }

        if (!session.TryGetGuild(guildId, out var guild))
        {
            var guildError = $"Guild {guildId.Value} is not available in the current GuildSaber session.";
            logger.Error(guildError);
            OnInitializationError.Invoke(guildError);
            return;
        }

        config.GuildId = guildId;
        config.ContextId = contextId;

        if (!await RefreshMemberStatsAsync(contextId, publishEvent: false, reportInitializationError: true))
            return;

        Initialized = true;
        Errored = false;

        // Things can subscribe and check for the Initialized property, so this shall be last.
        session.SetCurrentGuildContext(guild, contextId);
    }

    public Task<bool> RefreshCurrentMemberStatsAsync()
        => RefreshMemberStatsAsync(config.ContextId);

    public Task<bool> RefreshMemberStatsAsync(ContextId contextId)
        => RefreshMemberStatsAsync(contextId, publishEvent: true, reportInitializationError: false);

    private async Task<bool> RefreshMemberStatsAsync(
        ContextId contextId, bool publishEvent, bool reportInitializationError)
    {
        if (!session.HasLocalPlayer)
        {
            logger.Info("Local player is not loaded.");
            return false;
        }

        var levelsResponse = await client.LevelStats.GetByPlayerIdAsync(session.PlayerId, contextId);
        if (!levelsResponse.TryGetValue(out var levels, out var error))
        {
            logger.Error($"Failed to fetch levels: {error}.");
            if (reportInitializationError)
                OnInitializationError.Invoke(error);

            return false;
        }

        if (levels == null) return false;

        var pointsResponse = await client.ContextStats.GetByPlayerIdAsync(session.PlayerId, contextId);
        if (!pointsResponse.TryGetValue(out var contextStats, out error))
        {
            logger.Error($"Failed to fetch context stats: {error}.");
            if (reportInitializationError)
                OnInitializationError.Invoke(error);

            return false;
        }

        if (contextStats == null) return false;

        session.SetMemberStats(contextId, levels, contextStats.Value);

        if (publishEvent)
            OnMemberStatsRefreshed.Invoke(contextId);

        return true;
    }
}