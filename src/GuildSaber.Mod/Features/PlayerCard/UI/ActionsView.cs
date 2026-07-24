using System;
using System.Collections.Immutable;
using System.Linq;
using CP_SDK.UI.Components;
using CP_SDK.XUI;
using GuildSaber.Mod.Helpers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuildSaber.Mod.Features.PlayerCard.UI;

internal sealed class PlayerCardActionsView : XUIVLayout
{
    private readonly PlayerCardGuildPicker _guildPicker;
    private readonly PlayerCardResources _resources;
    private readonly Action<PlayerCardMessage> _send;
    private PlayerCardActions? _actions;
    private XUIDropdown _contextDropdown = null!;
    private GuildSelector _guildSelector = null!;
    private bool _rendering;

    private PlayerCardActionsView(
        PlayerCardResources resources,
        PlayerCardGuildPicker guildPicker,
        Action<PlayerCardMessage> send) : base("PlayerCardActions")
    {
        _resources = resources;
        _guildPicker = guildPicker;
        _send = send;
        OnReady(Build);
    }

    public static Vector2 Size => new(55, 42);

    public static PlayerCardActionsView Make(
        PlayerCardResources resources,
        PlayerCardGuildPicker guildPicker,
        Action<PlayerCardMessage> send)
        => new(resources, guildPicker, send);

    public void Render(PlayerCardActions actions)
    {
        _actions = actions;
        _guildSelector.Render(actions.Guilds);
        var contexts = actions.Contexts.Select(x => x.Name).ToList();

        _rendering = true;
        try
        {
            _contextDropdown.SetOptions(contexts);
            var selected = actions.Contexts
                .Where(x => x.Id == actions.CurrentContextId)
                .Select(x => x.Name)
                .FirstOrDefault();
            _contextDropdown.SetValue(selected ?? contexts.FirstOrDefault() ?? string.Empty, false);
        }
        finally
        {
            _rendering = false;
        }
    }

    private void Build(CHOrVLayout layout)
    {
        _guildSelector = GuildSelector.Make(_resources.DownArrowTexture, _resources.GsWhiteLogoTexture);
        _guildSelector.GuildSelected += id => _send(new PlayerCardMessage.SelectGuild(id));
        _guildSelector.PickerRequested += OpenGuildPicker;

        XUIVLayout.Make(
                XUIText.Make("Select a guild or context").SetColor(Color.yellow),
                _guildSelector,
                XUIHLayout.Make(
                    XUISecondaryButton.Make("Card settings")
                        .SetWidth(24).SetHeight(5)
                        .OnClick(() => _send(new PlayerCardMessage.OpenSettings())),
                    XUISecondaryButton.Make("Playlists")
                        .SetWidth(20).SetHeight(5)
                        .OnClick(() => _send(new PlayerCardMessage.OpenPlaylists()))),
                XUIDropdown.Make()
                    .Bind(ref _contextDropdown)
                    .OnValueChanged(OnContextSelected))
            .BuildUI(layout.transform);
    }

    private void OpenGuildPicker()
    {
        if (_actions is not { } actions) return;
        _guildPicker.Show(actions.Guilds, id => _send(new PlayerCardMessage.SelectGuild(id)));
    }

    private void OnContextSelected(int index, string _)
    {
        if (_rendering || _actions is not { } actions || index < 0 || index >= actions.Contexts.Length) return;
        _send(new PlayerCardMessage.SelectContext(actions.Contexts[index].Id));
    }
}

internal sealed class GuildSelector : XUIHLayout
{
    private readonly Texture2D _placeholder;
    private GuildIconButton _firstGuild = null!;
    private GuildIconButton _secondGuild = null!;

    private GuildSelector(Texture2D downArrow, Texture2D placeholder) : base("GuildSelector")
    {
        _placeholder = placeholder;
        OnReady(element =>
        {
            _firstGuild = GuildIconButton.Make(id => GuildSelected?.Invoke(id));
            _secondGuild = GuildIconButton.Make(id => GuildSelected?.Invoke(id));

            Make(
                    _firstGuild,
                    _secondGuild,
                    XUIIconButton.Make(() => PickerRequested?.Invoke())
                        .SetSprite(Sprite.Create(
                            downArrow,
                            new Rect(0, 0, downArrow.width, downArrow.height),
                            Vector2.zero))
                        .SetWidth(10)
                        .SetHeight(10)
                        .OnReady(x => x.transform.localRotation = Quaternion.Euler(0, 0, 90)))
                .BuildUI(element.transform);

            SetBackgroundColor(Color.black.WithAlpha(1f));
        });
    }

    public event Action<GuildId>? GuildSelected;
    public event Action? PickerRequested;

    public static GuildSelector Make(Texture2D downArrow, Texture2D placeholder) => new(downArrow, placeholder);

    public void Render(ImmutableArray<PlayerCardGuild> guilds)
    {
        _firstGuild.Render(guilds.Length > 0 ? guilds[0] : null, _placeholder);
        _secondGuild.Render(guilds.Length > 1 ? guilds[1] : null, _placeholder);
    }
}

internal sealed class GuildIconButton : XUIIconButton
{
    private GuildId _guildId;
    private Sprite? _sprite;
    private Texture2D? _texture;

    private GuildIconButton(Action<GuildId>? selected) : base("GuildIconButton", null)
        => OnClick(() => selected?.Invoke(_guildId));

    public static GuildIconButton Make(Action<GuildId>? selected = null) => new(selected);

    public void Render(PlayerCardGuild? guild, Texture2D placeholder)
    {
        SetActive(guild.HasValue);
        if (guild is not { } value) return;

        _guildId = value.Id;
        var texture = value.Icon ?? placeholder;
        if (_texture != texture)
        {
            if (_sprite != null) Object.Destroy(_sprite);
            _texture = texture;
            _sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                Vector2.zero);
        }

        SetSprite(_sprite!);
        SetWidth(8).SetHeight(8);
    }
}