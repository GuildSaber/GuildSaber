using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CP_SDK_BS.Game;
using GuildSaber.Api.Features.Guilds.Achievements.Http;
using GuildSaber.Common.Helpers;
using GuildSaber.CSharpClient.Routes.Guilds.Members.AchievementStats;
using GuildSaber.Mod.Features.GuildSaber;
using GuildSaber.Mod.Features.GuildSaber.Caching;
using GuildSaber.Mod.Features.GuildSaber.Runtime;
using GuildSaber.Mod.Features.PlayerCard.UI;
using GuildSaber.Mod.Features.PlayerCard.UI.Components;
using GuildSaber.Mod.Features.PlaylistDownloader.UI;
using GuildSaber.Mod.Helpers;
using UnityEngine;
using Zenject;

namespace GuildSaber.Mod.Features.PlayerCard;

public sealed class PlayerCardManager(
    GuildSaberManager guildSaberManager,
    GuildAssetCache assetCache,
    GuildSaberConfig config,
    PlayerCardView view,
    PlayerCardSettings settings,
    PlaylistDownloaderCoordinator playlistDownloader,
    PlayerCardPlayTime playTime,
    Logger logger
) : IInitializable, IDisposable
{
    private PlayerCardPlacement _placement;
    private PlayerCardSession? _session;

    private enum PlayerCardPage
    {
        Ready,
        Actions
    }

    private sealed record PlayerCardSession(
        GuildSaberSnapshot Snapshot,
        PlayerCardPage Page,
        Texture2D? Avatar,
        Texture2D? GuildIcon,
        ImmutableDictionary<GuildId, Texture2D> GuildIcons
    );

    public void Initialize()
    {
        config.PlayerCard.Migrate();

        guildSaberManager.StateChanged += OnRuntimeStateChanged;
        view.ActionRequested += OnActionRequested;
        view.TransformChanged += OnTransformChanged;
        settings.SettingsChanged += OnSettingsChanged;
        playTime.OnTimeUpdate += view.RenderTimer;

        Logic.OnSceneChange += OnSceneChanged;

        view.SetHandleVisible(config.PlayerCard.ShowHandle);
        view.RenderTimer(playTime.Current);

        OnSceneChanged(Logic.ActiveScene);
        OnRuntimeStateChanged(guildSaberManager.State);
    }

    public void Dispose()
    {
        _session = null;

        guildSaberManager.StateChanged -= OnRuntimeStateChanged;
        view.ActionRequested -= OnActionRequested;
        view.TransformChanged -= OnTransformChanged;
        settings.SettingsChanged -= OnSettingsChanged;
        playTime.OnTimeUpdate -= view.RenderTimer;

        Logic.OnSceneChange -= OnSceneChanged;
    }

    private void OnRuntimeStateChanged(GuildSaberRuntimeState state)
    {
        _session = null;
        switch (state)
        {
            case GuildSaberRuntimeState.Loading:
                view.Render(new PlayerCardState.Loading());
                break;
            case GuildSaberRuntimeState.AccountRequired:
                view.Render(new PlayerCardState.Unavailable(
                    "Create or link your GuildSaber account to use the player card.",
                    CanRetry: true));
                break;
            case GuildSaberRuntimeState.NoGuilds:
                view.Render(new PlayerCardState.Unavailable(
                    "You have not joined a guild yet. Join one on GuildSaber, then retry.",
                    CanRetry: true));
                break;
            case GuildSaberRuntimeState.Failed(var message, var canRetry):
                logger.Warn($"GuildSaber runtime error: {message}");
                view.Render(new PlayerCardState.Unavailable(message, canRetry));
                break;
            case GuildSaberRuntimeState.Ready(var snapshot):
                _session = new PlayerCardSession(
                    snapshot,
                    PlayerCardPage.Ready,
                    Avatar: null,
                    GuildIcon: null,
                    ImmutableDictionary<GuildId, Texture2D>.Empty);
                SetPageAndRender(PlayerCardPage.Ready);
                break;
        }

        // We must refresh the settings in case they are already opened but the runtime state changed.  
        RenderSettings();
    }

    private void OnActionRequested(PlayerCardActionMessage actionMessage)
    {
        switch (actionMessage)
        {
            case PlayerCardActionMessage.OpenActions:
                SetPageAndRender(PlayerCardPage.Actions);
                break;
            case PlayerCardActionMessage.CloseActions:
                SetPageAndRender(PlayerCardPage.Ready);
                break;
            case PlayerCardActionMessage.SelectGuild(var guildId):
                SelectGuild(guildId);
                break;
            case PlayerCardActionMessage.SelectContext(var contextId) when _session is { } session:
                Observe(
                    guildSaberManager.SelectGuildAsync(
                        session.Snapshot.CurrentGuildExtended.Guild.Id,
                        contextId),
                    "selecting a guild context");
                break;
            case PlayerCardActionMessage.OpenSettings:
                RenderSettings();
                settings.Present();
                SetPageAndRender(PlayerCardPage.Ready);
                break;
            case PlayerCardActionMessage.OpenPlaylists when _session is not null:
                playlistDownloader.Present();
                SetPageAndRender(PlayerCardPage.Ready);
                break;
            case PlayerCardActionMessage.OpenWebsite:
                OpenWebsite();
                break;
            case PlayerCardActionMessage.Retry:
                Observe(guildSaberManager.ReInitializeAsync(), "retrying GuildSaber initialization");
                break;
        }
    }

    private void OnSettingsChanged(PlayerCardSettingsMessage message)
    {
        switch (message)
        {
            case PlayerCardSettingsMessage.SetEnabled(var value):
                config.PlayerCard.Enabled = value;
                ApplyVisibility();
                break;
            case PlayerCardSettingsMessage.SetShowOrderedAchievements(var value):
                config.PlayerCard.ShowOrderedAchievements = value;
                break;
            case PlayerCardSettingsMessage.SetShowHandle(var value):
                config.PlayerCard.ShowHandle = value;
                view.SetHandleVisible(value);
                break;
            case PlayerCardSettingsMessage.SetColorMode(var value):
                config.PlayerCard.SetColorMode(value);

                /* Changing the color mode changes the settings layout, so we must rerender its layout.
                 * Could have been at the end of this function, but not desirable since changing colors would spam rerenders. */
                RenderSettings();
                break;
            case PlayerCardSettingsMessage.SetMainColor(var value):
                config.PlayerCard.ColorSettings.MainCardColor = value;
                break;
            case PlayerCardSettingsMessage.SetGradientStart(var value):
                config.PlayerCard.ColorSettings.GradientColor0 = value;
                break;
            case PlayerCardSettingsMessage.SetGradientEnd(var value):
                config.PlayerCard.ColorSettings.GradientColor1 = value;
                break;
            case PlayerCardSettingsMessage.ResetTimer:
                playTime.Reset();
                break;
            case PlayerCardSettingsMessage.ResetMenuPosition:
                config.PlayerCard.Transforms.Menu = new PlayerCardConfig().Transforms.Menu;
                if (_placement == PlayerCardPlacement.Menu) view.SetTransform(config.PlayerCard.Transforms.Menu);
                break;
            case PlayerCardSettingsMessage.ResetGameplayPosition:
                config.PlayerCard.Transforms.InSong = new PlayerCardConfig().Transforms.InSong;
                if (_placement is PlayerCardPlacement.GameplayRunning or PlayerCardPlacement.GameplayPaused)
                    view.SetTransform(config.PlayerCard.Transforms.InSong);
                break;
        }

        if (_session is { } session)
            Render(session);
    }

    private void SetPageAndRender(PlayerCardPage page)
    {
        if (_session is not { } session)
            return;

        session = session with { Page = page };
        _session = session;

        Render(session);

        switch (page)
        {
            case PlayerCardPage.Ready when session.Avatar is null || session.GuildIcon is null:
                Observe(LoadReadyAssets(session), "loading the player card images");
                break;
            case PlayerCardPage.Actions when session.GuildIcons.IsEmpty:
                Observe(LoadGuildIcons(session), "loading guild icons");
                break;
        }
    }

    private void Render(PlayerCardSession session)
        => view.Render(session.Page switch
        {
            PlayerCardPage.Ready => new PlayerCardState.Ready(PlayerCardReady.Create(
                snapshot: session.Snapshot,
                config: config.PlayerCard,
                avatar: session.Avatar,
                guildIcon: session.GuildIcon,
                canCustomize: CanCustomize(session.Snapshot))),
            PlayerCardPage.Actions => new PlayerCardState.Actions(PlayerCardActionData.Create(
                snapshot: session.Snapshot,
                guildIcons: session.GuildIcons)),
            _ => throw new ArgumentOutOfRangeException()
        });

    public void SetPaused(bool paused)
    {
        if (Logic.ActiveScene != Logic.ESceneType.Playing)
            return;

        _placement = paused ? PlayerCardPlacement.GameplayPaused : PlayerCardPlacement.GameplayRunning;
        ApplyVisibility();
    }

    private async Task LoadGuildIcons(PlayerCardSession session)
    {
        var icons = await Task.WhenAll(session.Snapshot.AvailableGuilds
            .Select(async guild => (
                guild.Guild.Id,
                Icon: await assetCache.GetOrFetchRoundedGuildIcon(guild.Guild.Id)))
        );

        if (!ReferenceEquals(session, _session))
            return;

        session = session with
        {
            GuildIcons = icons
                .Where(x => x.Icon != null)
                .ToImmutableDictionary(x => x.Id, x => x.Icon!)
        };

        _session = session;
        Render(session);
    }

    private async Task LoadReadyAssets(PlayerCardSession session)
    {
        var snapshot = session.Snapshot;
        var (avatar, guildIcon) = await (
                assetCache.GetOrFetchPlayerAvatar(
                    snapshot.PlayerId,
                    snapshot.PlayerExtended.Player.PlayerInfo.AvatarUrl),
                assetCache.GetOrFetchRoundedGuildIcon(snapshot.CurrentGuildExtended.Guild.Id))
            .WhenAll();

        if (!ReferenceEquals(session, _session)) return;

        session = session with
        {
            Avatar = avatar,
            GuildIcon = guildIcon
        };
        _session = session;
        Render(session);
    }

    private void RenderSettings() => settings.Render(
        PlayerCardSettingsState.Create(config.PlayerCard, _session is { } session && CanCustomize(session.Snapshot)));

    private void SelectGuild(GuildId guildId)
    {
        var guild = _session?.Snapshot.AvailableGuilds.FirstOrDefault(x => x.Guild.Id == guildId);
        if (guild is null) return;

        Observe(guildSaberManager.SelectGuildAsync(guild), "selecting a guild");
    }

    private void OnSceneChanged(Logic.ESceneType scene)
    {
        _placement = scene switch
        {
            Logic.ESceneType.Menu => PlayerCardPlacement.Menu,
            Logic.ESceneType.Playing => PlayerCardPlacement.GameplayRunning,
            _ => PlayerCardPlacement.Inactive
        };

        switch (_placement)
        {
            case PlayerCardPlacement.Menu:
                view.SetTransform(config.PlayerCard.Transforms.Menu);
                break;
            case PlayerCardPlacement.GameplayRunning:
                view.SetTransform(config.PlayerCard.Transforms.InSong);
                break;
        }

        view.SetPlayTimeVisible(_placement == PlayerCardPlacement.Menu);
        ApplyVisibility();
    }

    private void ApplyVisibility() => view.SetActive(
        config.PlayerCard.Enabled && _placement is PlayerCardPlacement.Menu or PlayerCardPlacement.GameplayPaused);

    private void OnTransformChanged(CardTransform transform)
    {
        switch (_placement)
        {
            case PlayerCardPlacement.Menu:
                config.PlayerCard.Transforms.Menu = transform;
                break;
            case PlayerCardPlacement.GameplayRunning or PlayerCardPlacement.GameplayPaused:
                config.PlayerCard.Transforms.InSong = transform;
                break;
        }
    }

    private void OpenWebsite()
    {
        try
        {
            Process.Start(config.ApiEnv.ToWebsiteUri().ToString());
        }
        catch (Exception exception)
        {
            logger.Error($"Failed to open the GuildSaber website: {exception}");
        }
    }

    private static bool CanCustomize(GuildSaberSnapshot snapshot)
    {
        const int requiredAchievementOrder = 30;
        return snapshot.AchievementStats.GetGlobalAchievement()?.Progression switch
        {
            _ when (ulong)snapshot.PlayerExtended.Player.PlayerLinkedAccounts.BeatLeaderId
                is 76561198846350061 or 76561198126131670 => true,
            AchievementResponses.AchievementProgression.Ordered
            {
                Order: >= requiredAchievementOrder
            } => true,
            _ => false
        };
    }

    private void Observe(Task task, string operation) => _ = ObserveAsync(task, operation);

    private async Task ObserveAsync(Task task, string operation)
    {
        try
        {
            await task;
        }
        catch (Exception exception)
        {
            logger.Error($"Error while {operation}: {exception}");
        }
    }
}
