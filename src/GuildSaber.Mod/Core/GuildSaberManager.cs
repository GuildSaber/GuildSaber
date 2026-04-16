using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BS_Utils.Gameplay;
using GuildSaber.Api.Features.Guilds;
using GuildSaber.Api.Features.Guilds.Members.ContextStats;
using GuildSaber.Api.Features.Players;
using GuildSaber.Common.Services.BeatLeader.Models.Responses;
using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using GuildSaber.Common.StrongTypes;
using GuildSaber.CSharpClient;
using IPA.Config.Data;
using SiraUtil.Logging;
using Zenject;

namespace GuildSaber.Mod.Core;

[SuppressMessage("ReSharper", "AsyncVoidMethod")]
public class GuildSaberManager(GuildSaberClient client, SiraLog logger, ModData modData) : IInitializable
{
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

        var playerIdResult = await client.Players.GetExtendedByIdAsync(new PlayerId(1));
        if (!playerIdResult.TryGetValue(out var player, out var error))
        {
            logger.Error($"Failed to fetch PlayerId: {error}. " +
                         "Invoking OnPlayerIdFetched with null and terminating Initialize.");
            OnInitializationError("Failed to fetch PlayerId from GuildSaber.");
            return;
        }

        if (player == null)
        {
            return;
        }

        modData.Player = player;

        List<GuildResponses.Guild> guilds = new List<GuildResponses.Guild>();
        foreach (var member in modData.Player.Members)
        {
            var guildResponse = await client.Guilds.GetByIdAsync(new GuildId(member.GuildId));

            if (!guildResponse.TryGetValue(out var guild, out error))
            {
                logger.Error($"Failed to fetch guild: {error}. ");
                OnInitializationError("Failed to fetch PlayerId from GuildSaber.");
            }

            if (guild == null) return;
                
            guilds.Add(guild);
        }

        modData.Guilds = guilds;
    }

    //public event Action<PlayerId?> OnPlayerIdFetched = _ => { };
    public event Action<string> OnInitializationError = _ => { };

    public async void SelectGuild(int guildId, int contextId)
    {
        if (modData.Player == null) return;
        
        var levelsResponse = await client.LevelStats.GetByPlayerIdAsync(modData.Player.Player.Id, contextId);
        if (!levelsResponse.TryGetValue(out var levels, out var error))
        {
            logger.Error($"Failed to fetch levels: {error}.");
            return;
        }

        if (levels == null) return;

        modData.PlayerLevels = levels;

        var pointsResponse = await client.ContextStats.GetByPlayerIdAsync(modData.Player.Player.Id, contextId);
        if (!pointsResponse.TryGetValue(out var contextStats, out error))
        {
            logger.Error($"Failed to fetch context stats: {error}.");
            return;
        }

        if (contextStats == null) return;
        
        modData.PlayerPoints = contextStats.Value.SimplePointsWithRank;
    }
}