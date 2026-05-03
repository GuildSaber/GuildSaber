using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BS_Utils.Gameplay;
using GuildSaber.Api.Features.Guilds;
using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using GuildSaber.Common.StrongTypes;
using GuildSaber.CSharpClient;
using GuildSaber.Mod.Configurations;
using SiraUtil.Logging;
using Zenject;

namespace GuildSaber.Mod.Core;

[SuppressMessage("ReSharper", "AsyncVoidMethod")]
public class GuildSaberManager(GuildSaberClient client, SiraLog logger, GuildSaberCache cache, PluginConfig config)
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

        if (userInfo.platform != UserInfo.Platform.Steam)
        {
            logger.Warn($"User platform is {userInfo.platform}, only Steam is supported. " +
                        "Invoking OnPlayerIdFetched with null and terminating Initialize.");
            OnInitializationError("Only Steam platform is supported.");
            return;
        }

        BeatLeaderId.TryParse(userInfo.platformUserId, out var playerBeatLeaderId);

        var playerIdResult = await client.Players.LookupPlayerIdByBeatLeaderIdAsync(playerBeatLeaderId);
        if (!playerIdResult.TryGetValue(out var playerId, out var error))
        {
            logger.Error($"Failed to fetch PlayerId: {error}. " +
                         "Invoking OnPlayerIdFetched with null and terminating Initialize.");
            OnInitializationError("Failed to fetch PlayerId from GuildSaber.");
            return;
        }

        if (playerId == null) return;

        var extendedPlayerIdResult = await client.Players.GetExtendedByIdAsync(playerId.Value);
        if (!extendedPlayerIdResult.TryGetValue(out var extendedPlayer, out error))
        {
            logger.Error($"Failed to fetch extended player: {error}");
            logger.Error("Terminating");
            OnInitializationError.Invoke(error);
            return;
        }

        if (extendedPlayer == null) return;

        cache.PlayerExtended = extendedPlayer;

        List<GuildResponses.GuildExtended> guilds = [];
        foreach (var member in cache.PlayerExtended.Members)
        {
            var guildResponse = await client.Guilds.GetExtendedByIdAsync(new GuildId(member.GuildId));

            if (!guildResponse.TryGetValue(out var guild, out error))
            {
                logger.Error($"Failed to fetch guild: {error}. ");
                OnInitializationError("Failed to fetch PlayerId from GuildSaber.");
            }

            if (guild == null) return;

            guilds.Add(guild);
            cache.GuildsExtended[guild.Guild.Id] = guild;
        }

        if (!cache.GuildsExtended.TryGetValue(config.PlayerCard.GuildId, out var selectedGuild))
        {
            config.PlayerCard.GuildId = guilds[0].Guild.Id;
            config.PlayerCard.ContextId = guilds[0].Contexts[0].Id;
            selectedGuild = guilds[0];
        }

        SelectGuild(config.PlayerCard.GuildId, selectedGuild.Contexts[0].Id);
    }

    //public event Action<PlayerId?> OnPlayerIdFetched = _ => { };
    public event Action<string> OnInitializationError = _ => { };
    public event Action OnInitializationFinished = () => { };

    public void SetGuild(GuildResponses.GuildExtended guild)
    {
        config.PlayerCard.GuildId = guild.Guild.Id;
        config.PlayerCard.ContextId = guild.Contexts[0].Id;

        SelectGuild(guild.Guild.Id, guild.Contexts[0].Id);
    }

    public async void SelectGuild(GuildId guildId, ContextId contextId)
    {
        if (cache.PlayerExtended == null) return;

        var categoryResponse = await client.Categories.GetAllByGuildIdAsync(guildId);
        if (!categoryResponse.TryGetValue(out var categories, out var error))
        {
            logger.Error($"Failed to fetch categories: {error}");
            OnInitializationError.Invoke(error);
            return;
        }

        var levelsResponse = await client.LevelStats.GetByPlayerIdAsync(cache.PlayerExtended.Player.Id, contextId);
        if (!levelsResponse.TryGetValue(out var levels, out error))
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