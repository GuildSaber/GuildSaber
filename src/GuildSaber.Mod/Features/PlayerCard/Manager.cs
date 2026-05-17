using BeatSaberMarkupLanguage.FloatingScreen;
using GuildSaber.Mod.Features.GuildSaber;
using GuildSaber.Mod.Features.PlayerCard.UI;
using HMUI;
using Zenject;

namespace GuildSaber.Mod.Features.PlayerCard;

internal class PlayerCardManager(
    GuildSaberManager manager,
    PlayerCardView playerCardView,
    [Inject(Id = Constants.CardFloatingPanelId)] FloatingScreen cardFloatingScreen,
    Logger logger
) : IInitializable
{
    public void Initialize()
    {
        manager.OnInitializationStarted += () => { playerCardView.DisplayCard(PlayerCardView.EDisplayMode.Loading); };
        manager.OnInitializationFinished += playerCardView.RefreshCard;
        manager.OnInitializationError += error =>
        {
            logger.Warn($"GuildSaberManager initialization error: {error}");
            playerCardView.DisplayCard(PlayerCardView.EDisplayMode.Error);
        };
        manager.OnNoGuildError += () =>
        {
            logger.Warn("GuildSaberManager initialization error: User is not in a guild.");
            playerCardView.DisplayCard(PlayerCardView.EDisplayMode.DidntJoinGuild);
        };

        cardFloatingScreen.name = "PlayerCardFloatingScreen";
        cardFloatingScreen.SetRootViewController(playerCardView, ViewController.AnimationType.In);

        // In case GuildSaberManager is already initialized before PlayerCardManager, we directly refresh the card.
        // playerCardView.RefreshCard();
    }
}