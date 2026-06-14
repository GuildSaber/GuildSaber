using System;
using System.Collections.Generic;
using CP_SDK.XUI;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Mod.Features.GuildSaber.Caching;
using UnityEngine;

namespace GuildSaber.Mod.Features.PlayerCard.UI.Components.GuildSelector;

public class GuildIconButton : XUIIconButton
{
    private readonly GuildAssetCache _guildAssetCache;
    private readonly List<Action<GuildId>> _onGuildSelected = [];

    //////////////////////////////////////////////////////
    //////////////////////////////////////////////////////

    private GuildId _guildID;

    public GuildIconButton(GuildAssetCache guildAssetCache, Texture2D whiteLogo, Action<GuildId>? onGuildSelected)
        : base("GuildIconButton", null)
    {
        _guildAssetCache = guildAssetCache;

        var sprite = Sprite.Create(whiteLogo,
            new Rect(0, 0, whiteLogo.width, whiteLogo.width),
            Vector2.zero);

        SetSprite(sprite);
        OnClick(OnButtonClicked);
        if (onGuildSelected != null) OnGuildSelected(onGuildSelected);
    }

    public static GuildIconButton
        Make(GuildAssetCache guildAssetCache, Texture2D whiteLogo, Action<GuildId>? onGuildSelected)
        => new(guildAssetCache, whiteLogo, onGuildSelected);


    //////////////////////////////////////////////////////
    //////////////////////////////////////////////////////

    public async void SetGuild(GuildId guildId)
    {
        SetActive(true);

        _guildID = guildId;

        var roundedLogo = await _guildAssetCache.GetOrFetchRoundedGuildIcon(guildId);
        if (roundedLogo == null)
            return;

        SetSprite(Sprite.Create(roundedLogo, new Rect(0, 0, roundedLogo.width, roundedLogo.height), Vector2.zero));
        SetWidth(8);
        SetHeight(8);
    }

    //////////////////////////////////////////////////////
    //////////////////////////////////////////////////////

    public GuildIconButton OnGuildSelected(Action<GuildId> x)
    {
        _onGuildSelected.Add(x);
        return this;
    }

    private void OnButtonClicked()
    {
        foreach (var item in _onGuildSelected) item.Invoke(_guildID);
    }
}