using BeatSaberMarkupLanguage.FloatingScreen;
using CP_SDK_BS.Game;
using GuildSaber.Mod.Features.GuildSaber;
using GuildSaber.Mod.Features.GuildSaber.Runtime;
using GuildSaber.Mod.Features.PlayerCard.Patches;
using GuildSaber.Mod.Features.PlayerCard.UI;
using HMUI;
using ModestTree;
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
        Logic.OnSceneChange += SceneChanged;
        
        PauseHook.EventGamePaused += GamePaused;
        PauseHook.EventGameResumed += GameResumed;
        
        cardFloatingScreen.name = "PlayerCardFloatingScreen";
        cardFloatingScreen.SetRootViewController(playerCardView, ViewController.AnimationType.In);
    }

    private void SceneChanged(Logic.ESceneType scene)
    {
        switch (scene)
        {
            default:
            case Logic.ESceneType.Menu:
                cardFloatingScreen.gameObject.SetActive(true);
                break;
            case Logic.ESceneType.Playing:
                cardFloatingScreen.gameObject.SetActive(false);
                break;
        }
    }

    private void GamePaused() => cardFloatingScreen.gameObject.SetActive(true);
    private void GameResumed() => cardFloatingScreen.gameObject.SetActive(false);
}