using BeatSaberMarkupLanguage.FloatingScreen;
using GuildSaber.Mod.Features.GuildSaber;
using GuildSaber.Mod.Features.GuildSaber.Runtime;
using GuildSaber.Mod.Features.PlayerCard.UI;
using HMUI;
using Zenject;

namespace GuildSaber.Mod.Features.PlayerCard;

internal class PlayerCardManager(
    GuildSaberManager manager,
    GuildSaberSession session,
    GuildSaberConfig config,
    PlayerCardView playerCardView,
    [Inject(Id = Constants.CardFloatingPanelId)] FloatingScreen cardFloatingScreen,
    Logger logger
) : IInitializable
{
    public void Initialize()
    {
        if (manager.Initialized) playerCardView.RefreshCard();
        session.CurrentGuildContextChanged += (_, _) => playerCardView.RefreshCard();

        manager.OnGuildSelectionStarted += () => playerCardView.DisplayCard(PlayerCardView.EDisplayMode.Loading);
        manager.OnMemberStatsRefreshed += contextId =>
        {
            if (contextId == config.ContextId)
                playerCardView.RefreshMemberStats();
        };
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
    }
}