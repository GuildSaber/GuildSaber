using System;
using BeatSaberMarkupLanguage.FloatingScreen;
using CP_SDK_BS.Game;
using GuildSaber.Mod.Features.GuildSaber;
using GuildSaber.Mod.Features.GuildSaber.Runtime;
using GuildSaber.Mod.Features.GuildSaber.Settings;
using GuildSaber.Mod.Features.PlayerCard.UI;
using GuildSaber.Mod.Features.PlayerCard.UI.Settings;
using HMUI;
using Zenject;

namespace GuildSaber.Mod.Features.PlayerCard;

public class PlayerCardManager(
    GuildSaberManager manager,
    GuildSaberSession session,
    GuildSaberConfig config,
    GuildSaberSettingsView guildSaberSettingsView,
    PlayerCardView playerCardView,
    PlayerCardSettingsMainView cardSettingsView,
    [Inject(Id = Constants.CardFloatingPanelId)] FloatingScreen cardFloatingScreen,
    Logger logger
) : IInitializable, IDisposable
{
    public void Dispose()
    {
        Logic.OnSceneChange -= OnSceneChanged;
        guildSaberSettingsView.OnResetCardMenuPosition -= OnResetCardMenuPosition;
        guildSaberSettingsView.OnResetCardInSongPosition -= OnResetInSongMenuPosition;
        cardSettingsView.OnResetMenuPosition -= OnResetCardMenuPosition;
        cardSettingsView.OnResetInSongPosition -= OnResetInSongMenuPosition;
    }

    public void Initialize()
    {
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
        cardFloatingScreen.HandleReleased += (_, x) =>
        {
            if (Logic.ActiveScene != Logic.ESceneType.Playing)
                config.PlayerCard.Transforms.Menu = new CardTransform(x.Position, x.Rotation);
            else
                config.PlayerCard.Transforms.InSong = new CardTransform(x.Position, x.Rotation);
        };

        Logic.OnSceneChange += OnSceneChanged;
        guildSaberSettingsView.OnResetCardMenuPosition += OnResetCardMenuPosition;
        guildSaberSettingsView.OnResetCardInSongPosition += OnResetInSongMenuPosition;
        cardSettingsView.OnResetMenuPosition += OnResetCardMenuPosition;
        cardSettingsView.OnResetInSongPosition += OnResetInSongMenuPosition;

        if (manager.Errored) playerCardView.DisplayCard(PlayerCardView.EDisplayMode.Error);
        else if (manager.Initialized) playerCardView.RefreshCard();
    }

    private void OnResetCardMenuPosition()
    {
        config.PlayerCard.Transforms.Menu = new PlayerCardConfig().Transforms.Menu;
        if (Logic.ActiveScene == Logic.ESceneType.Menu)
            SetCardToMenuTransform();
    }

    private void OnResetInSongMenuPosition()
    {
        config.PlayerCard.Transforms.InSong = new PlayerCardConfig().Transforms.InSong;
        if (Logic.ActiveScene == Logic.ESceneType.Playing)
            SetCardToInSongTransform();
    }

    private void OnSceneChanged(Logic.ESceneType x)
    {
        switch (x)
        {
            case Logic.ESceneType.Menu:
                SetCardToMenuTransform();
                SetFloatingScreenActive(true);
                playerCardView.SetTimerVisibility(true);
                break;
            case Logic.ESceneType.Playing:
                SetCardToInSongTransform();
                SetFloatingScreenActive(false);
                playerCardView.SetTimerVisibility(false);
                break;
            case Logic.ESceneType.None:
            default: return;
        }
    }

    public void SetCardToMenuTransform()
    {
        cardFloatingScreen.transform.position = config.PlayerCard.Transforms.Menu.Position;
        cardFloatingScreen.transform.rotation = config.PlayerCard.Transforms.Menu.Rotation;
    }

    public void SetCardToInSongTransform()
    {
        cardFloatingScreen.transform.position = config.PlayerCard.Transforms.InSong.Position;
        cardFloatingScreen.transform.rotation = config.PlayerCard.Transforms.InSong.Rotation;
    }

    public void SetFloatingScreenActive(bool active) => cardFloatingScreen.gameObject.SetActive(active);
}