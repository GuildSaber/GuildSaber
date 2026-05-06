using System;
using System.Collections.Generic;
using CP_SDK.XUI;
using GuildSaber.Common.StrongTypes;
using GuildSaber.CSharpClient;
using GuildSaber.Mod.Features.GuildSaber;
using GuildSaber.Mod.Helpers;
using UnityEngine;

namespace GuildSaber.Mod.Features.PlayerCard.UI.Components.GuildSelector;

public class GuildIconButton : XUIIconButton
{
    private readonly GuildSaberClient _client;
    private readonly GuildSaberCache _guildSaberData;
    private readonly List<Action<GuildId>> _onGuildSelected = [];

    //////////////////////////////////////////////////////
    /////////////////////////////////////////////////////

    private GuildId _guildID;

    public GuildIconButton(GuildSaberCache guildSaberData, Texture2D whiteLogo, GuildSaberClient client,
                           Action<GuildId>? onGuildSelected)
        : base("GuildIconButton", null)
    {
        _guildSaberData = guildSaberData;
        _client = client;

        var sprite = Sprite.Create(whiteLogo,
            new Rect(0, 0, whiteLogo.width, whiteLogo.width),
            Vector2.zero);

        SetSprite(sprite);
        OnClick(OnButtonClicked);
        if (onGuildSelected != null) OnGuildSelected(onGuildSelected);
    }

    public static GuildIconButton
        Make(GuildSaberCache guildSaberData, Texture2D whiteLogo, GuildSaberClient client,
             Action<GuildId>? onGuildSelected)
        => new(guildSaberData, whiteLogo, client, onGuildSelected);


    //////////////////////////////////////////////////////
    /////////////////////////////////////////////////////

    public async void SetGuild(GuildId guildId)
    {
        SetActive(true);

        _guildID = guildId;

        var guildLogo = await _guildSaberData.FetchGuildIconTexture(guildId, _client);
        if (guildLogo == null)
            return;

        var roundedLogo = await TextureUtils.CreateRoundedTextureAsync(guildLogo, guildLogo.width * 0.1f);

        SetSprite(Sprite.Create(roundedLogo, new Rect(0, 0, guildLogo.width, guildLogo.height), Vector2.zero));
        SetWidth(8);
        SetHeight(8);
    }

    //////////////////////////////////////////////////////
    /////////////////////////////////////////////////////

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