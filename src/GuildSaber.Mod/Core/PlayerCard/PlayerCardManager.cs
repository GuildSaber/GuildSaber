using BeatSaberMarkupLanguage.FloatingScreen;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Mod.PlayerCard.UI;
using HMUI;
using SiraUtil.Logging;
using Zenject;

namespace GuildSaber.Mod.Core.PlayerCard;

internal class PlayerCardManager(
    GuildSaberManager manager, 
    PlayerCardView playerCardView,
    [Inject(Id = Constants.CardFloatingPanelId)] FloatingScreen cardFloatingScreen,
    SiraLog logger
    ) : IInitializable
{
    public void Initialize()
    {
        manager.OnInitializationError += error => { logger.Warn($"GuildSaberManager initialization error: {error}"); };

        cardFloatingScreen.name = "PlayerCardFloatingScreen";
        cardFloatingScreen.SetRootViewController(playerCardView, ViewController.AnimationType.In);
    }

    private void OnPlayerIdFetched(PlayerId? playerId)
        => logger.Info($"Received PlayerId: {(playerId.HasValue ? playerId.Value.ToString() : "null")}");
}