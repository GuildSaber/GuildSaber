using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CP_SDK_BS.Game;
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
        Summary,
        Actions
    }

    private sealed record PlayerCardSession(
        GuildSaberSnapshot Snapshot,
        PlayerCardPage Page,
        Texture2D? Avatar,
        Texture2D? GuildIcon,
        ImmutableDictionary<GuildId, Texture2D> GuildIcons);

    public void Initialize()
    {
        config.PlayerCard.Migrate();
        guildSaberManager.StateChanged += OnRuntimeStateChanged;
        view.MessageSent += OnMessage;
        view.TransformChanged += OnTransformChanged;
        settings.MessageSent += OnSettingsMessage;
        playTime.OnTimeUpdate += view.RenderPlayTime;
        Logic.OnSceneChange += OnSceneChanged;

        view.SetHandleVisible(config.PlayerCard.ShowHandle);
        view.RenderPlayTime(playTime.Current);
        OnSceneChanged(Logic.ActiveScene);
        OnRuntimeStateChanged(guildSaberManager.State);
    }

    public void Dispose()
    {
        _session = null;
        guildSaberManager.StateChanged -= OnRuntimeStateChanged;
        view.MessageSent -= OnMessage;
        view.TransformChanged -= OnTransformChanged;
        settings.MessageSent -= OnSettingsMessage;
        playTime.OnTimeUpdate -= view.RenderPlayTime;
        Logic.OnSceneChange -= OnSceneChanged;
    }

    public void SetPaused(bool paused)
    {
        if (Logic.ActiveScene != Logic.ESceneType.Playing) return;
        _placement = paused ? PlayerCardPlacement.GameplayPaused : PlayerCardPlacement.GameplayRunning;
        ApplyVisibility();
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
                    PlayerCardPage.Summary,
                    Avatar: null,
                    GuildIcon: null,
                    ImmutableDictionary<GuildId, Texture2D>.Empty);
                ShowPage(PlayerCardPage.Summary);
                break;
        }

        RenderSettings();
    }

    private void OnMessage(PlayerCardMessage message)
    {
        switch (message)
        {
            case PlayerCardMessage.OpenActions:
                ShowPage(PlayerCardPage.Actions);
                break;
            case PlayerCardMessage.CloseActions:
                ShowPage(PlayerCardPage.Summary);
                break;
            case PlayerCardMessage.SelectGuild(var guildId):
                SelectGuild(guildId);
                break;
            case PlayerCardMessage.SelectContext(var contextId) when _session is { } session:
                Observe(
                    guildSaberManager.SelectGuildAsync(
                        session.Snapshot.CurrentGuildExtended.Guild.Id,
                        contextId),
                    "selecting a guild context");
                break;
            case PlayerCardMessage.OpenSettings:
                RenderSettings();
                settings.Present();
                ShowPage(PlayerCardPage.Summary);
                break;
            case PlayerCardMessage.OpenPlaylists when _session is not null:
                playlistDownloader.Present();
                ShowPage(PlayerCardPage.Summary);
                break;
            case PlayerCardMessage.OpenWebsite:
                OpenWebsite();
                break;
            case PlayerCardMessage.Retry:
                Observe(guildSaberManager.ReInitializeAsync(), "retrying GuildSaber initialization");
                break;
        }
    }

    private void OnSettingsMessage(PlayerCardSettingsMessage message)
    {
        switch (message)
        {
            case PlayerCardSettingsMessage.SetEnabled(var value):
                config.PlayerCard.Enabled = value;
                ApplyVisibility();
                break;
            case PlayerCardSettingsMessage.SetShowProgress(var value):
                config.PlayerCard.CategoryLevelViewEnabled = value;
                break;
            case PlayerCardSettingsMessage.SetShowHandle(var value):
                config.PlayerCard.ShowHandle = value;
                view.SetHandleVisible(value);
                break;
            case PlayerCardSettingsMessage.SetColorMode(var value):
                config.PlayerCard.SetColorMode(value);
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

        RenderSettings();
        if (_session is { Page: PlayerCardPage.Summary } session) Render(session);
    }

    private void ShowPage(PlayerCardPage page)
    {
        if (_session is not { } session) return;

        session = session with { Page = page };
        _session = session;
        Render(session);

        if (page == PlayerCardPage.Summary && (session.Avatar is null || session.GuildIcon is null))
            Observe(LoadSummaryAssets(session), "loading the player card images");
        else if (page == PlayerCardPage.Actions && session.GuildIcons.IsEmpty)
            Observe(LoadGuildIcons(session), "loading guild icons");
    }

    private void Render(PlayerCardSession session)
        => view.Render(session.Page switch
        {
            PlayerCardPage.Summary => new PlayerCardState.Summary(PlayerCardSummary.Create(
                session.Snapshot,
                config.PlayerCard,
                session.Avatar,
                session.GuildIcon,
                CanCustomize(session.Snapshot))),
            PlayerCardPage.Actions => new PlayerCardState.Actions(PlayerCardActions.Create(
                session.Snapshot,
                session.GuildIcons)),
            _ => throw new ArgumentOutOfRangeException()
        });

    private async Task LoadGuildIcons(PlayerCardSession session)
    {
        var icons = await Task.WhenAll(session.Snapshot.AvailableGuilds.Select(async guild => (
            guild.Guild.Id,
            Icon: await assetCache.GetOrFetchRoundedGuildIcon(guild.Guild.Id))));

        if (!ReferenceEquals(session, _session)) return;

        session = session with
        {
            GuildIcons = icons
                .Where(x => x.Icon != null)
                .ToImmutableDictionary(x => x.Id, x => x.Icon!)
        };
        _session = session;
        Render(session);
    }

    private async Task LoadSummaryAssets(PlayerCardSession session)
    {
        var snapshot = session.Snapshot;
        var avatarTask = assetCache.GetOrFetchPlayerAvatar(
            snapshot.PlayerId,
            snapshot.PlayerExtended.Player.PlayerInfo.AvatarUrl);
        var guildIconTask = assetCache.GetOrFetchRoundedGuildIcon(
            snapshot.CurrentGuildExtended.Guild.Id);
        await Task.WhenAll(avatarTask, guildIconTask);

        if (!ReferenceEquals(session, _session)) return;

        session = session with
        {
            Avatar = await avatarTask,
            GuildIcon = await guildIconTask
        };
        _session = session;
        Render(session);
    }

    private void RenderSettings()
        => settings.Render(PlayerCardSettingsState.Create(
            config.PlayerCard,
            _session is { } session && CanCustomize(session.Snapshot)));

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
        config.PlayerCard.Enabled && _placement is PlayerCardPlacement.Menu
                                                   or PlayerCardPlacement.GameplayPaused);

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
        const int requiredLevel = 30;
        return snapshot.LevelStats.Where(x => x.Level.CategoryId is null && !x.IsLocked)
                .LastOrDefault(x => x.IsCompleted)?.Level.Order switch
            {
                _ when (ulong)snapshot.PlayerExtended.Player.PlayerLinkedAccounts.BeatLeaderId
                    is 76561198846350061 or 76561198126131670 => true,
                >= requiredLevel => true,
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