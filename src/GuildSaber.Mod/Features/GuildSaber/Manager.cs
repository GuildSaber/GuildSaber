using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using BS_Utils.Gameplay;
using GuildSaber.Api.Features.Guilds;
using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using GuildSaber.Common.StrongTypes;
using GuildSaber.CSharpClient;
using Zenject;

namespace GuildSaber.Mod.Features.GuildSaber;

[SuppressMessage("ReSharper", "AsyncVoidMethod")]
public class GuildSaberManager(GuildSaberClient client, Logger logger, GuildSaberCache cache, GuildSaberConfig config)
    : IInitializable
{
    public bool Initialized;

    public async void Initialize()
    {
        logger.Info("Initializing GuildSaberManager...");

        var userInfo = await GetUserInfo.GetUserAsync();
        if (userInfo == null)
        {
            logger.Warn("UserInfo is null, cannot fetch PlayerId. " +
                        "Invoking OnPlayerIdFetched with null and terminating Initialize.");
            OnInitializationError("Failed to retrieve user information.");
            return;
        }

        if (!BeatLeaderId.TryParse(userInfo.platformUserId).TryGetValue(out var beatLeaderId, out var error))
        {
            logger.Warn($"Failed to parse BeatLeaderId: {error}. " +
                        "Invoking OnPlayerIdFetched with null and terminating Initialize.");
            OnInitializationError("Failed to parse BeatLeaderId from user information.");
            return;
        }

        logger.Info($"Fetched BeatLeaderId: {beatLeaderId} of kind {beatLeaderId.Kind}.");

        if (!(await client.Players.LookupPlayerIdByBeatLeaderIdAsync(beatLeaderId))
            .TryGetValue(out var playerId, out error))
        {
            logger.Error($"Failed to fetch PlayerId: {error}. " +
                         "Invoking OnPlayerIdFetched with null and terminating Initialize.");
            OnInitializationError("Failed to fetch PlayerId from GuildSaber.");
            return;
        }

        if (playerId == null)
        {
            logger.Warn("PlayerId is null, user does not have an account on GuildSaber. " +
                        "Invoking OnPlayerIdFetched with null and terminating Initialize.");
            OnInitializationError("User does not have an account on GuildSaber.");
            return;
        }

        var extendedPlayerIdResult = await client.Players.GetExtendedByIdAsync(playerId.Value);
        if (!extendedPlayerIdResult.TryGetValue(out var extendedPlayer, out error))
        {
            logger.Error($"Failed to fetch extended player: {error}");
            logger.Error("Terminating");
            OnInitializationError.Invoke(error);
            return;
        }

        cache.PlayerExtended = extendedPlayer;
        var guildExtendeds = await Task.WhenAll(cache.PlayerExtended!.Members.Select(async member =>
        {
            var guildResponse = await client.Guilds.GetExtendedByIdAsync(new GuildId(member.GuildId));
            if (!guildResponse.TryGetValue(out var guild, out var guildError))
            {
                logger.Error($"Failed to fetch guild: {guildError}. ");
                OnInitializationError("Failed to fetch PlayerId from GuildSaber.");
                return null;
            }

            return guild;
        }));

        foreach (var guildExtended in guildExtendeds.OfType<GuildResponses.GuildExtended>())
            cache.GuildsExtended[guildExtended.Guild.Id] = guildExtended;

        if (guildExtendeds.Length == 0)
        {
            logger.Warn("Player is not a member of any guild. " +
                        "Invoking OnPlayerIdFetched with null and terminating Initialize.");
            OnNoGuildError();
            return;
        }

        // Make sure the config selected guild and context exists, if not select the first one.
        if (!cache.GuildsExtended.TryGetValue(config.GuildId, out var selectedGuild))
        {
            var guildExtended = cache.GuildsExtended.First().Value;
            config.GuildId = guildExtended.Guild.Id;
            config.ContextId = guildExtended.Contexts[0].Id;
            selectedGuild = guildExtended;
        }
        else
        {
            var guildExtended = cache.GuildsExtended[config.GuildId];
            if (guildExtended.Contexts.All(x => x.Id != config.ContextId))
                config.ContextId = guildExtended.Contexts[0].Id;
        }

        SelectGuild(config.GuildId, selectedGuild.Contexts[0].Id);
    }

    public event Action<string> OnInitializationError = _ => { };
    public event Action OnNoGuildError = () => { };
    public event Action OnInitializationFinished = () => { };
    public event Action OnInitializationStarted = () => { };

    public void SetGuild(GuildResponses.GuildExtended guild)
    {
        config.GuildId = guild.Guild.Id;
        config.ContextId = guild.Contexts[0].Id;

        SelectGuild(guild.Guild.Id, guild.Contexts[0].Id);
    }

    public async void SelectGuild(GuildId guildId, ContextId contextId)
    {
        OnInitializationStarted.Invoke();
        
        if (cache.PlayerExtended == null) return;

        var levelsResponse = await client.LevelStats.GetByPlayerIdAsync(cache.PlayerExtended.Player.Id, contextId);
        if (!levelsResponse.TryGetValue(out var levels, out var error))
        {
            logger.Error($"Failed to fetch levels: {error}.");
            OnInitializationError.Invoke(error);
            return;
        }

        if (levels == null) return;

        cache.MemberLevelStats[contextId] = levels;

        var pointsResponse = await client.ContextStats.GetByPlayerIdAsync(cache.PlayerExtended.Player.Id, contextId);
        if (!pointsResponse.TryGetValue(out var contextStats, out error))
        {
            logger.Error($"Failed to fetch context stats: {error}.");
            OnInitializationError.Invoke(error);
            return;
        }

        if (contextStats == null) return;
        cache.MemberContextStats[contextId] = contextStats.Value;

        Initialized = true;
        OnInitializationFinished.Invoke();
    }
}