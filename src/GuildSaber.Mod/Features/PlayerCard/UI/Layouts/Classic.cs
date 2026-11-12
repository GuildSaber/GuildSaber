using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using CP_SDK.XUI;
using GuildSaber.Common.Helpers;
using GuildSaber.Mod.Features.PlayerCard.UI.Components;
using GuildSaber.Mod.Helpers;
using HMUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GuildSaber.Mod.Features.PlayerCard.UI.Layouts;

internal sealed class ClassicPlayerCardLayout(
    PlayerCardResources resources,
    Action<PlayerCardActionMessage> send) : IPlayerCardLayout
{
    private const float BaseHeight = 32;
    private const float ContentWidth = 78;
    private const float HeaderWidth = ContentWidth - 22;
    private const float StatsHeight = 11;
    private const float StatsWidth = 32;

    private static readonly Color _botGold = new(1, 215f / 255, 0);
    private readonly List<XUIText> _pointTexts = [];
    private XUIText _achievement = null!;
    private Texture2D? _avatar;
    private Sprite? _avatarSprite;
    private ImageView _border = null!;
    private XUIVLayout _borderLayout = null!;
    private PlayerCardCategories _categories = null!;
    private ImageView _divider = null!;
    private Texture2D? _guildIcon;
    private XUIIconButton _guildImage = null!;
    private Sprite? _guildSprite;
    private XUIHLayout _header = null!;
    private XUIIconButton _playerImage = null!;
    private XUIText _playerName = null!;
    private XUIText _playerPasses = null!;
    private XUIVLayout _points = null!;
    private XUIVLayout _root = null!;
    private XUIText _time = null!;
    private PlayerCardTrophies _trophies = null!;

    public void Build(Transform parent)
    {
        _categories = PlayerCardCategories.Make();
        _trophies = PlayerCardTrophies.Make(resources);
        _trophies.SetHeight(7).SetPadding(2, 0, 0, 0);

        XUIVLayout.Make(
                BuildHeader(),
                XUIImage.Make()
                    .OnReady(x =>
                    {
                        x.LElement.flexibleHeight = 0;
                        x.ImageC.preserveAspect = false;
                        _divider = x.ImageC as ImageView
                                   ?? throw new InvalidOperationException("The divider is not an ImageView.");
                    })
                    .SetWidth(ContentWidth)
                    .SetHeight(0.35f),
                _trophies,
                _categories)
            .SetBackground(true)
            .SetBackgroundColor(Color.black.WithAlpha(0.8f))
            .OnReady(x =>
            {
                x.CSizeFitter.horizontalFit = x.CSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
                x.HOrVLayoutGroup.childForceExpandHeight = false;
                x.HOrVLayoutGroup.childAlignment = TextAnchor.UpperCenter;
            })
            .SetPadding(1)
            .SetSpacing(0)
            .Bind(ref _root)
            .BuildUI(parent);

        XUIVLayout.Make()
            .SetBackground(true)
            .Bind(ref _borderLayout)
            .OnReady(x =>
            {
                _border = x.gameObject.GetComponent<ImageView>();
                _border.material = resources.BorderMaterial;
                _border.sprite = resources.BorderSprite;
                x.CSizeFitter.verticalFit = x.CSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            })
            .BuildUI(parent);
    }

    public void Render(PlayerCardReady ready)
    {
        SetAvatar(ready.Avatar ?? resources.GsWhiteLogoTexture);
        SetGuildIcon(ready.GuildIcon ?? resources.GsWhiteLogoTexture);
        _playerName.SetText(ready.PlayerName.Truncate(20)).SetColor(ready.Palette.Accent);
        _playerPasses.SetText(ready.Passes).SetColor(_botGold);
        _achievement.SetText(ready.AchievementName).SetColor(ready.Palette.Accent);
        RenderPoints(ready.Points);
        _categories.SetActive(ready.ShowOrderedAchievements);
        _categories.Render(ready.Progress, ready.Palette.Accent);
        var width = GetContentWidth(ready);
        _header.SetWidth(width);
        _divider.GetComponent<LayoutElement>().preferredWidth = width;
        _trophies.Render(ready.Trophies, width);
        ApplyPalette(ready.Palette);
    }

    public void RenderPlayTime(PlayerCardPlayTime.TimeOnlyLite time)
        => _time.SetText($"{time.Hours:00}:{time.Minutes:00}:{time.Seconds:00}");

    public void SetPlayTimeVisible(bool visible) => _time.SetActive(visible);

    public void SetActive(bool active)
    {
        _root.SetActive(active);
        _borderLayout.SetActive(active);
        if (active)
        {
            _playerImage.Element.IconImageC.rectTransform.localScale = Vector3.one;
            _guildImage.Element.IconImageC.rectTransform.localScale = Vector3.one;
        }
    }

    public Vector2 GetSize(PlayerCardReady ready)
        => new(GetContentWidth(ready) + 2, BaseHeight + (ready.ShowOrderedAchievements ? _categories.Height : 0));

    public void Dispose()
    {
        if (_avatarSprite != null) Object.Destroy(_avatarSprite);
        if (_guildSprite != null) Object.Destroy(_guildSprite);
    }

    private float GetContentWidth(PlayerCardReady ready)
        => ready.ShowOrderedAchievements ? Mathf.Max(ContentWidth, _categories.Width) : ContentWidth;

    private XUIHLayout BuildHeader()
        => XUIHLayout.Make(
                XUIVLayout.Make(
                        XUIIconButton.Make(() => send(new PlayerCardActionMessage.OpenActions()))
                            .SetWidth(16)
                            .SetHeight(16)
                            .Bind(ref _playerImage),
                        XUIText.Make("00:00:00")
                            .Bind(ref _time)
                            .SetFontSize(3.2f)
                            .SetAlign(TextAlignmentOptions.Center)
                            .SetWrapping(false)
                            .OnReady(x =>
                            {
                                x.LElement.minHeight = 0;
                                x.LElement.preferredWidth = 16;
                                x.LElement.preferredHeight = 3.5f;
                            }))
                    .SetSpacing(0.5f)
                    .OnReady(x =>
                    {
                        x.CSizeFitter.horizontalFit =
                            x.CSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
                        x.LElement.flexibleHeight = 0;
                        x.HOrVLayoutGroup.childAlignment = TextAnchor.UpperLeft;
                    })
                    .SetPadding(1, 0, 0, 1)
                    .SetWidth(20)
                    .SetHeight(22),
                XUIVLayout.Make(
                        XUIHLayout.Make(
                                XUIText.Make(string.Empty)
                                    .Bind(ref _playerName)
                                    .SetStyle(FontStyles.Bold)
                                    .SetFontSize(6.5f)
                                    .SetFontSizes(3.5f, 6.5f)
                                    .SetAlign(TextAlignmentOptions.MidlineLeft)
                                    .SetWrapping(false)
                                    .SetOverflowMode(TextOverflowModes.Ellipsis)
                                    .OnReady(x =>
                                    {
                                        x.TMProUGUI.enableAutoSizing = true;
                                        x.LElement.minHeight = 0;
                                        x.LElement.preferredWidth = HeaderWidth - 10;
                                        x.LElement.preferredHeight = 9;
                                        x.LElement.flexibleWidth = 1;
                                    }),
                                XUIIconButton.Make(() => send(new PlayerCardActionMessage.OpenActions()))
                                    .Bind(ref _guildImage)
                                    .SetWidth(8)
                                    .SetHeight(8))
                            .OnReady(x =>
                            {
                                x.CSizeFitter.horizontalFit =
                                    x.CSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
                                x.LElement.flexibleHeight = 0;
                                x.HOrVLayoutGroup.childForceExpandWidth = false;
                                x.HOrVLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
                            })
                            .SetWidth(HeaderWidth)
                            .SetHeight(9)
                            .SetPadding(0)
                            .SetSpacing(2),
                        BuildStats())
                    .OnReady(x =>
                    {
                        x.CSizeFitter.horizontalFit =
                            x.CSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
                        x.LElement.flexibleHeight = 0;
                        x.LElement.flexibleWidth = 1;
                        x.HOrVLayoutGroup.childForceExpandHeight = false;
                        x.HOrVLayoutGroup.childForceExpandWidth = true;
                        x.HOrVLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
                    })
                    .SetWidth(HeaderWidth)
                    .SetHeight(22)
                    .SetPadding(0)
                    .SetSpacing(0))
            .OnReady(x =>
            {
                x.CSizeFitter.horizontalFit = x.CSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
                x.LElement.flexibleHeight = 0;
                x.HOrVLayoutGroup.childForceExpandHeight = false;
                x.HOrVLayoutGroup.childForceExpandWidth = false;
                x.HOrVLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
            })
            .SetWidth(ContentWidth)
            .SetHeight(22)
            .SetPadding(0)
            .SetSpacing(2)
            .Bind(ref _header);

    private XUIHLayout BuildStats()
        => XUIHLayout.Make(
                XUIVLayout.Make(
                        XUIVLayout.Make()
                            .Bind(ref _points)
                            .OnReady(x =>
                            {
                                x.CSizeFitter.horizontalFit =
                                    x.CSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
                                x.LElement.flexibleHeight = 0;
                                x.HOrVLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
                            })
                            .SetWidth(StatsWidth)
                            .SetPadding(0)
                            .SetSpacing(0),
                        XUIText.Make(string.Empty)
                            .Bind(ref _playerPasses)
                            .SetFontSize(4)
                            .SetFontSizes(2, 4)
                            .SetAlign(TextAlignmentOptions.MidlineLeft)
                            .SetWrapping(false)
                            .SetOverflowMode(TextOverflowModes.Ellipsis)
                            .OnReady(x =>
                            {
                                x.TMProUGUI.enableAutoSizing = true;
                                x.LElement.minHeight = 0;
                                x.LElement.preferredWidth = StatsWidth;
                                x.LElement.flexibleWidth = 0;
                                x.LElement.flexibleHeight = 0;
                            }))
                    .OnReady(x =>
                    {
                        x.CSizeFitter.horizontalFit =
                            x.CSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
                        x.LElement.flexibleHeight = 0;
                        x.HOrVLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
                    })
                    .SetWidth(StatsWidth)
                    .SetHeight(StatsHeight)
                    .SetPadding(0)
                    .SetSpacing(0),
                XUIText.Make(string.Empty)
                    .Bind(ref _achievement)
                    .SetStyle(FontStyles.Bold | FontStyles.Italic)
                    .SetFontSize(7.4f)
                    .SetFontSizes(3.5f, 7.4f)
                    .SetAlign(TextAlignmentOptions.Midline)
                    .SetWrapping(false)
                    .SetOverflowMode(TextOverflowModes.Ellipsis)
                    .OnReady(x =>
                    {
                        x.TMProUGUI.enableAutoSizing = true;
                        x.LElement.minHeight = 0;
                        x.LElement.preferredWidth = HeaderWidth - StatsWidth - 2;
                        x.LElement.preferredHeight = StatsHeight + 2;
                        x.LElement.flexibleWidth = 1;
                    }))
            .OnReady(x =>
            {
                x.CSizeFitter.horizontalFit = x.CSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
                x.LElement.flexibleHeight = 0;
                x.HOrVLayoutGroup.childForceExpandWidth = false;
                x.HOrVLayoutGroup.childAlignment = TextAnchor.UpperLeft;
            })
            .SetWidth(HeaderWidth)
            .SetHeight(StatsHeight + 2)
            .SetPadding(0)
            .SetSpacing(2);

    private void RenderPoints(ImmutableArray<PlayerCardPoint> points)
    {
        var rowHeight = StatsHeight / (points.Length + 1);
        _points.SetHeight(rowHeight * points.Length);
        _playerPasses.Element.LElement.preferredHeight = rowHeight;

        while (_pointTexts.Count < points.Length)
        {
            var text = XUIText.Make(string.Empty);
            text.SetFontSize(4);
            text.SetFontSizes(2, 4);
            text.SetAlign(TextAlignmentOptions.MidlineLeft);
            text.SetWrapping(false);
            text.SetOverflowMode(TextOverflowModes.Ellipsis);
            text.OnReady(x =>
            {
                x.TMProUGUI.enableAutoSizing = true;
                x.LElement.minHeight = 0;
                x.LElement.preferredWidth = StatsWidth;
                x.LElement.flexibleWidth = 0;
                x.LElement.flexibleHeight = 0;
            });
            text.BuildUI(_points.Element.transform);
            _pointTexts.Add(text);
        }

        for (var i = 0; i < _pointTexts.Count; i++)
        {
            _pointTexts[i].SetActive(i < points.Length);
            if (i < points.Length)
            {
                _pointTexts[i].Element.LElement.preferredHeight = rowHeight;
                _pointTexts[i]
                    .SetText(points[i].Text)
                    .SetColor(_botGold);
            }
        }
    }

    private void SetAvatar(Texture2D avatar)
    {
        if (_avatar == avatar) return;

        if (_avatarSprite != null) Object.Destroy(_avatarSprite);
        _avatar = avatar;
        _avatarSprite = CreateSprite(avatar);
        _playerImage.SetSprite(_avatarSprite);
    }

    private void SetGuildIcon(Texture2D icon)
    {
        if (_guildIcon == icon) return;

        if (_guildSprite != null) Object.Destroy(_guildSprite);
        _guildIcon = icon;
        _guildSprite = CreateSprite(icon);
        _guildImage.SetSprite(_guildSprite);
    }

    private void ApplyPalette(PlayerCardPalette palette)
    {
        ApplyPalette(_divider, palette);
        ApplyPalette(_border, palette);
    }

    private static void ApplyPalette(ImageView image, PlayerCardPalette palette)
    {
        switch (palette)
        {
            case PlayerCardPalette.Solid(var color):
                image.gradient = false;
                image.color = image.color0 = image.color1 = color;
                break;
            case PlayerCardPalette.Gradient { Start: var start, End: var end }:
                image.gradient = true;
                image.color = Color.white;
                image.color0 = start;
                image.color1 = end;
                break;
        }
    }

    private static Sprite CreateSprite(Texture2D texture)
        => Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
}