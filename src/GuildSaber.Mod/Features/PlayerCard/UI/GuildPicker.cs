using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using CP_SDK_BS.UI;
using CP_SDK.XUI;
using GuildSaber.Mod.Features.Common.UI;
using GuildSaber.Mod.Helpers;
using GuildSaber.Mod.Resources;
using HMUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace GuildSaber.Mod.Features.PlayerCard.UI;

internal sealed class PlayerCardGuildPicker : SimpleFlowCoordinator
{
    [Inject] private readonly PlayerCardGuildPickerView _view = null!;
    private Action<GuildId>? _selected;

    protected override string Title => "Select guild";
    protected override ViewController GetMainViewController() => _view;

    protected override void OnCreation() => _view.GuildSelected += OnGuildSelected;

    public void Show(ImmutableArray<PlayerCardGuild> guilds, Action<GuildId> selected)
    {
        _selected = selected;
        _view.Render(guilds);
        Present();
    }

    private void OnGuildSelected(GuildId guildId)
    {
        _selected?.Invoke(guildId);
        Dismiss();
    }
}

internal sealed class PlayerCardGuildPickerView : ViewController<PlayerCardGuildPickerView>
{
    private readonly List<PlayerCardGuildButton> _buttons = [];
    [Inject(Id = ResourceMap.TekoMedium)] private readonly TMP_FontAsset _font = null!;
    [Inject(Id = ResourceMap.GsWhiteLogo)] private readonly Texture2D _placeholder = null!;
    private XUIVScrollView _guildList = null!;
    private ImmutableArray<PlayerCardGuild> _guilds = [];
    private bool _isReady;

    public event Action<GuildId>? GuildSelected;

    public void Render(ImmutableArray<PlayerCardGuild> guilds)
    {
        _guilds = guilds;
        if (_isReady) RenderGuilds();
    }

    protected override void OnViewCreation()
        => XUIVLayout.Make(
                XUIHLayout.Make(XUIVScrollView.Make().Bind(ref _guildList))
                    .SetHeight(80)
                    .OnReady(x =>
                    {
                        x.CSizeFitter.verticalFit = x.CSizeFitter.horizontalFit =
                            ContentSizeFitter.FitMode.Unconstrained;
                        x.HOrVLayoutGroup.childForceExpandHeight = x.HOrVLayoutGroup.childForceExpandWidth = true;
                    }))
            .SetWidth(100)
            .SetHeight(80)
            .SetBackground(true)
            .SetBackgroundColor(Color.clear)
            .OnReady(_ =>
            {
                _isReady = true;
                RenderGuilds();
            })
            .BuildUI(RTransform);

    private void RenderGuilds()
    {
        while (_buttons.Count < _guilds.Length)
        {
            var button = PlayerCardGuildButton.Make(_font);
            button.SetWidth(70).SetHeight(10);
            button.Clicked += id => GuildSelected?.Invoke(id);
            button.BuildUI(_guildList.Element.Container);
            _buttons.Add(button);
        }

        for (var i = 0; i < _buttons.Count; i++)
        {
            _buttons[i].SetActive(i < _guilds.Length);
            if (i < _guilds.Length) _buttons[i].Render(_guilds[i], _placeholder);
        }
    }
}

internal sealed class PlayerCardGuildButton : XUISecondaryButton
{
    private readonly GuildIconButton _guildIcon;
    private readonly XUIText _guildName;
    private PlayerCardGuild _guild;

    private PlayerCardGuildButton(TMP_FontAsset font) : base("", font)
    {
        _guildIcon = GuildIconButton.Make();
        _guildName = XUIText.Make(string.Empty);

        OnReady(element =>
        {
            _guildName.SetMargins(0, 1, 0, 0)
                .OnReady(x =>
                {
                    x.LElement.ignoreLayout = true;
                    x.RTransform.sizeDelta = Vector2.zero;
                    x.RTransform.anchorMin = Vector2.zero;
                    x.RTransform.anchorMax = Vector2.one;
                });

            XUIHLayout.Make(
                    XUIVLayout.Make(_guildIcon).SetMinWidth(8),
                    XUIHLayout.Make(_guildName).SetMinWidth(50))
                .SetHeight(10)
                .SetMinHeight(8)
                .BuildUI(element.transform);
        });

        OnClick(() => Clicked?.Invoke(_guild.Id));
    }

    public event Action<GuildId>? Clicked;

    public static PlayerCardGuildButton Make(TMP_FontAsset font) => new(font);

    public override Color GetColor() => Color.black.WithAlpha(0.7f);

    public void Render(PlayerCardGuild guild, Texture2D placeholder)
    {
        _guild = guild;
        _guildIcon.Render(guild, placeholder);
        _guildName.SetText(guild.Name is { Length: > 32 } ? guild.Name[..30] + "..." : guild.Name);
        SetActive(true);
    }
}