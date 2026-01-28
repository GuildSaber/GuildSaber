using GuildSaber.Common.StrongTypes;
using SiraUtil.Logging;
using Zenject;

namespace GuildSaber.Mod.Core.PlayerCard;

internal class PlayerCardManager(GuildSaberManager manager, SiraLog logger) : IInitializable
{
    public void Initialize()
    {
        manager.OnPlayerIdFetched += OnPlayerIdFetched;
        manager.OnInitializationError += error => { logger.Warn($"GuildSaberManager initialization error: {error}"); };
    }

    private void OnPlayerIdFetched(PlayerId? playerId)
        => logger.Info($"Received PlayerId: {(playerId.HasValue ? playerId.Value.ToString() : "null")}");
}