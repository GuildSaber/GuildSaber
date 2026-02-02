using System;
using System.Diagnostics.CodeAnalysis;
using BS_Utils.Gameplay;
using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using GuildSaber.Common.StrongTypes;
using GuildSaber.CSharpClient;
using SiraUtil.Logging;
using Zenject;

namespace GuildSaber.Mod.Core;

[SuppressMessage("ReSharper", "AsyncVoidMethod")]
public class GuildSaberManager(GuildSaberClient client, SiraLog logger) : IInitializable
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

        var playerIdResult = await client.Players.LookupPlayerIdByBeatLeaderIdAsync(
            BeatLeaderId.TryParseUnsafe(userInfo.platformUserId).Value);
        if (!playerIdResult.TryGetValue(out var playerId, out var error))
        {
            logger.Error($"Failed to fetch PlayerId: {error}. " +
                         "Invoking OnPlayerIdFetched with null and terminating Initialize.");
            OnInitializationError("Failed to fetch PlayerId from GuildSaber.");
            return;
        }

        OnPlayerIdFetched(playerId);
    }

    public event Action<PlayerId?> OnPlayerIdFetched = _ => { };
    public event Action<string> OnInitializationError = _ => { };
}