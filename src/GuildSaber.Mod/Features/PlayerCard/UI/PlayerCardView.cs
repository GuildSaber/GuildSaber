using System;
using BeatSaberMarkupLanguage.FloatingScreen;
using CP_SDK_BS.UI;
using CP_SDK.XUI;
using GuildSaber.Mod.Features.PlayerCard.UI.Components;
using GuildSaber.Mod.Features.PlayerCard.UI.Layouts;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace GuildSaber.Mod.Features.PlayerCard.UI;

public sealed class PlayerCardView : ViewController<PlayerCardView>, IInitializable, IDisposable
{
    [Inject] private readonly PlayerCardGuildPicker _guildPicker = null!;
    [Inject] private readonly PlayerCardResources _resources = null!;
    [Inject(Id = Constants.CardFloatingPanelId)] private readonly FloatingScreen _screen = null!;

    private PlayerCardActionsView _actions = null!;
    private IPlayerCardLayout _layout = null!;

    private XUIVLayout _loading = null!;
    private XUIVLayout _message = null!;
    private XUIText _messageText = null!;

    private PlayerCardState? _pendingState;
    private PlayerCardPlayTime.TimeOnlyLite _pendingTime;
    private bool _playTimeVisible = true;
    private bool _ready;
    private XUISecondaryButton _retry = null!;

    public void Dispose()
    {
        _screen.HandleReleased -= OnHandleReleased;
        if (_ready) _layout.Dispose();
    }

    public void Initialize()
    {
        _screen.name = "PlayerCardFloatingScreen";
        _screen.SetRootViewController(this, AnimationType.In);
        _screen.HandleReleased += OnHandleReleased;
        DontDestroyOnLoad(_screen.gameObject);
    }

    public event Action<PlayerCardMessage>? MessageSent;
    public event Action<CardTransform>? TransformChanged;

    public void Render(PlayerCardState state)
    {
        if (!_ready)
        {
            _pendingState = state;
            return;
        }

        _layout.SetActive(state is PlayerCardState.Summary);
        _actions.SetActive(state is PlayerCardState.Actions);
        _loading.SetActive(state is PlayerCardState.Loading);
        _message.SetActive(state is PlayerCardState.Unavailable);

        switch (state)
        {
            case PlayerCardState.Loading:
                _screen.ScreenSize = new Vector2(50, 20);
                break;
            case PlayerCardState.Unavailable(var reason, var canRetry):
                _messageText.SetText(reason);
                _retry.SetActive(canRetry);
                _screen.ScreenSize = new Vector2(55, 36);
                break;
            case PlayerCardState.Summary(var summary):
                _layout.Render(summary);
                _screen.ScreenSize = _layout.GetSize(summary);
                break;
            case PlayerCardState.Actions(var actions):
                _actions.Render(actions);
                _screen.ScreenSize = PlayerCardActionsView.Size;
                break;
        }
    }

    public void RenderPlayTime(PlayerCardPlayTime.TimeOnlyLite timeOnlyLite)
    {
        _pendingTime = timeOnlyLite;
        if (_ready) _layout.RenderPlayTime(timeOnlyLite);
    }

    public void SetPlayTimeVisible(bool visible)
    {
        _playTimeVisible = visible;
        if (_ready) _layout.SetPlayTimeVisible(visible);
    }

    public void SetActive(bool value) => _screen.gameObject.SetActive(value);
    public void SetHandleVisible(bool value) => _screen.ShowHandle = value;

    public void SetTransform(CardTransform value)
    {
        _screen.transform.position = value.Position;
        _screen.transform.rotation = value.Rotation;
    }

    public void MoveToActiveScene()
        => SceneManager.MoveGameObjectToScene(_screen.gameObject, SceneManager.GetActiveScene());

    protected override void OnViewCreation()
    {
        _layout = new ClassicPlayerCardLayout(_resources, Send);
        _layout.Build(transform);

        _actions = PlayerCardActionsView.Make(_resources, _guildPicker, Send);
        _actions.BuildUI(transform);

        BuildMessage();
        BuildLoading();

        ModalContainerRTransform.localScale *= 0.6f;
        _ready = true;
        _layout.RenderPlayTime(_pendingTime);
        _layout.SetPlayTimeVisible(_playTimeVisible);
        if (_pendingState is { } state) Render(state);
    }

    private void BuildMessage()
        => XUIVLayout.Make(
                XUIText.Make(string.Empty).Bind(ref _messageText).SetColor(new Color(1, 0.5f, 0)),
                XUIHLayout.Make(
                    XUISecondaryButton.Make("Open in browser")
                        .SetWidth(30).SetHeight(4)
                        .OnClick(() => Send(new PlayerCardMessage.OpenWebsite())),
                    XUISecondaryButton.Make("Retry")
                        .Bind(ref _retry)
                        .SetWidth(20).SetHeight(4)
                        .OnClick(() => Send(new PlayerCardMessage.Retry()))))
            .Bind(ref _message)
            .BuildUI(transform);

    private void BuildLoading()
        => XUIVLayout.Make(XUILoadingIndicator.Make())
            .Bind(ref _loading)
            .BuildUI(transform);

    private void OnHandleReleased(object sender, FloatingScreenHandleEventArgs args)
        => TransformChanged?.Invoke(new CardTransform(args.Position, args.Rotation));

    private void Send(PlayerCardMessage message) => MessageSent?.Invoke(message);
}

internal interface IPlayerCardLayout : IDisposable
{
    void Build(Transform parent);
    void Render(PlayerCardSummary summary);
    void RenderPlayTime(PlayerCardPlayTime.TimeOnlyLite timeOnlyLite);
    void SetPlayTimeVisible(bool visible);
    void SetActive(bool active);
    Vector2 GetSize(PlayerCardSummary summary);
}

public abstract record PlayerCardMessage
{
    public sealed record OpenActions : PlayerCardMessage;
    public sealed record CloseActions : PlayerCardMessage;
    public sealed record SelectGuild(GuildId GuildId) : PlayerCardMessage;
    public sealed record SelectContext(ContextId ContextId) : PlayerCardMessage;
    public sealed record OpenSettings : PlayerCardMessage;
    public sealed record OpenPlaylists : PlayerCardMessage;
    public sealed record OpenWebsite : PlayerCardMessage;
    public sealed record Retry : PlayerCardMessage;
}