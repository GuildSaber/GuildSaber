using System;
using System.Collections.Immutable;
using System.Linq;
using CP_SDK.XUI;
using GuildSaber.Mod.Helpers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuildSaber.Mod.Features.PlayerCard.UI;

internal sealed class PlayerCardActionsView : XUIVLayout
{
    private readonly PlayerCardGuildPicker _guildPicker;
    private readonly Action<PlayerCardActionMessage> _send;

    private PlayerCardActionData? _actionData;
    private XUIDropdown _contextDropdown = null!;
    private GuildSelector _guildSelector = null!;
    private bool _rendering;

    public static Vector2 Size => new(55, 42);

    private PlayerCardActionsView(
        PlayerCardResources resources,
        PlayerCardGuildPicker guildPicker,
        Action<PlayerCardActionMessage> send) : base("PlayerCardActions")
    {
        _guildPicker = guildPicker;
        _send = send;

        OnReady(layout =>
        {
            _guildSelector = GuildSelector.Make(resources.DownArrowTexture, resources.GsWhiteLogoTexture);
            _guildSelector.GuildSelected += id => _send(new PlayerCardActionMessage.SelectGuild(id));
            _guildSelector.PickerRequested += OpenGuildPicker;

            XUIVLayout.Make(
                    XUIText.Make("Select a guild or context").SetColor(Color.yellow),
                    _guildSelector,
                    XUIHLayout.Make(
                        XUISecondaryButton.Make("Card settings")
                            .SetWidth(24).SetHeight(5)
                            .OnClick(() => _send(new PlayerCardActionMessage.OpenSettings())),
                        XUISecondaryButton.Make("Playlists")
                            .SetWidth(20).SetHeight(5)
                            .OnClick(() => _send(new PlayerCardActionMessage.OpenPlaylists()))),
                    XUIDropdown.Make()
                        .Bind(ref _contextDropdown)
                        .OnValueChanged(OnContextSelected))
                .BuildUI(layout.transform);
        });
    }

    public static PlayerCardActionsView Make(
        PlayerCardResources resources, PlayerCardGuildPicker guildPicker, Action<PlayerCardActionMessage> send)
        => new(resources, guildPicker, send);

    public void Render(PlayerCardActionData actionData)
    {
        _actionData = actionData;
        _guildSelector.Render(actionData.Guilds);
        var contexts = actionData.Contexts.Select(x => x.Name).ToList();

        _rendering = true;
        try
        {
            var selected = actionData.Contexts
                .Where(x => x.Id == actionData.CurrentContextId)
                .Select(x => x.Name)
                .FirstOrDefault();

            _contextDropdown.SetOptions(contexts);
            _contextDropdown.SetValue(selected ?? contexts.FirstOrDefault() ?? string.Empty, false);
        }
        finally
        {
            _rendering = false;
        }
    }

    private void OpenGuildPicker()
    {
        if (_actionData is not { } actionData) return;
        _guildPicker.Show(actionData.Guilds, id => _send(new PlayerCardActionMessage.SelectGuild(id)));
    }

    private void OnContextSelected(int index, string _)
    {
        if (_rendering || _actionData is not { } actionData || index < 0 || index >= actionData.Contexts.Length) return;
        _send(new PlayerCardActionMessage.SelectContext(actionData.Contexts[index].Id));
    }
}

internal sealed class GuildSelector : XUIHLayout
{
    public const int MaxGuildCount = 6;

    private readonly GuildIconButton[] _guildButtons;
    private readonly Texture2D _placeholder;

    public event Action<GuildId>? GuildSelected;
    public event Action? PickerRequested;

    private GuildSelector(Texture2D downArrow, Texture2D placeholder) : base("GuildSelector")
    {
        _placeholder = placeholder;
        _guildButtons = Enumerable
            .Range(0, MaxGuildCount)
            .Select(_ => GuildIconButton.Make(id => GuildSelected?.Invoke(id)))
            .ToArray();

        OnReady(element =>
        {
            Make([
                .._guildButtons,
                XUIIconButton.Make(() => PickerRequested?.Invoke())
                    .SetSprite(Sprite.Create(
                        texture: downArrow,
                        rect: new Rect(0, 0, downArrow.width, downArrow.height),
                        pivot: Vector2.zero))
                    .SetWidth(10)
                    .SetHeight(10)
                    .OnReady(x => x.transform.localRotation = Quaternion.Euler(0, 0, 90))
            ]).BuildUI(element.transform);

            SetBackgroundColor(Color.black.WithAlpha(1f));
        });
    }

    public void Render(ImmutableArray<PlayerCardGuild> guilds)
    {
        for (var index = 0; index < _guildButtons.Length; index++)
        {
            var playerCardGuild = index < guilds.Length ? guilds[index] : (PlayerCardGuild?)null;
            _guildButtons[index].Render(playerCardGuild, _placeholder);
        }
    }

    public static GuildSelector Make(Texture2D downArrow, Texture2D placeholder) => new(downArrow, placeholder);
}

internal sealed class GuildIconButton : XUIIconButton
{
    private GuildId _guildId;
    private Sprite? _sprite;
    private Texture2D? _texture;

    private GuildIconButton(Action<GuildId>? selected) : base("GuildIconButton", null)
        => OnClick(() => selected?.Invoke(_guildId));

    public void Render(PlayerCardGuild? guild, Texture2D placeholder)
    {
        SetActive(guild.HasValue);
        if (guild is not { } value) return;

        _guildId = value.Id;
        var texture = value.Icon ?? placeholder;
        if (_texture != texture)
        {
            if (_sprite != null)
                Object.Destroy(_sprite);

            _texture = texture;
            _sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                Vector2.zero);
        }

        SetSprite(_sprite);
        SetWidth(8).SetHeight(8);
    }

    public static GuildIconButton Make(Action<GuildId>? selected = null) => new(selected);
}