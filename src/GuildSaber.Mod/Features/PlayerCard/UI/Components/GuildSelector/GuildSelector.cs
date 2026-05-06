using System;
using System.Collections.Generic;
using System.Linq;
using CP_SDK.XUI;
using GuildSaber.Api.Features.Guilds;
using GuildSaber.Common.StrongTypes;
using GuildSaber.CSharpClient;
using GuildSaber.Mod.Features.GuildSaber;
using GuildSaber.Mod.Helpers;
using UnityEngine;

namespace GuildSaber.Mod.Features.PlayerCard.UI.Components.GuildSelector;

public class GuildSelector : XUIHLayout
{
    private readonly GuildSaberCache _guildSaberData;
    private readonly GuildSaberManager _guildSaberManager;
    private readonly GuildSelectorFlowCoordinator _guildSelectorFlowCoordinator;

    protected XUIIconButton ArrowButton = null!;

    ///////////////////////////////////////////////////////
    //////////////////////////////////////////////////////

    protected GuildIconButton Guild1 = null!;
    protected GuildIconButton Guild2 = null!;

    protected Action<GuildResponses.GuildExtended> OnGuildSelected = null!;

    public GuildSelector(
        GuildSelectorFlowCoordinator guildSelectorFlowCoordinator,
        Texture2D downArrowTexture,
        Texture2D whiteArrowTexture,
        GuildSaberCache guildSaberCache,
        GuildSaberManager guildSaberManager,
        GuildSaberClient client)
        : base("GuildSelector")
    {
        var whiteLogoTexture = whiteArrowTexture;
        var downArrowTexture1 = downArrowTexture;
        _guildSelectorFlowCoordinator = guildSelectorFlowCoordinator;
        _guildSaberData = guildSaberCache;
        _guildSaberManager = guildSaberManager;
        var client1 = client;

        OnReady(_ =>
        {
            Guild1 = new GuildIconButton(_guildSaberData, whiteLogoTexture, client1, OnIconGuildSelected);
            Guild2 = new GuildIconButton(_guildSaberData, whiteLogoTexture, client1, OnIconGuildSelected);

            Make(
                Guild1,
                Guild2,
                XUIIconButton.Make()
                    .Bind(ref ArrowButton)
                    .SetSprite(Sprite.Create(downArrowTexture1,
                        new Rect(0, 0, downArrowTexture1.width, downArrowTexture1.height),
                        Vector2.zero))
                    .OnClick(OnArrowButtonClicked)
                    .SetWidth(10)
                    .SetHeight(10)
                    .OnReady(x => x.transform.localRotation = Quaternion.Euler(0, 0, 90))
            ).BuildUI(Element.transform);

            SetBackgroundColor(new Color(26.0f / 255, 28.0f / 255, 30.0f / 255));

            try
            {
                Guild1.SetGuild(_guildSaberData.GuildsExtended.First().Value.Guild.Id);
                Guild2.SetGuild(_guildSaberData.GuildsExtended.ElementAt(1).Value.Guild.Id);
            }
            catch
            {
                // ignored
            }
        });
    }


    public class GuildIconButton : XUIIconButton
    {
        private readonly GuildSaberClient _client;
        private readonly GuildSaberCache _guildSaberData;
        private readonly List<Action<GuildId>> _onGuildSelected = [];
        private readonly Texture2D _whiteLogo;

        //////////////////////////////////////////////////////
        /////////////////////////////////////////////////////

        private GuildId _guildID;

        public GuildIconButton(GuildSaberCache guildSaberData, Texture2D whiteLogo, GuildSaberClient client,
                               Action<GuildId>? onGuildSelected) :
            base("GuildIconButton", null)
        {
            _guildSaberData = guildSaberData;
            _client = client;
            _whiteLogo = whiteLogo;

            var sprite = Sprite.Create(_whiteLogo,
                new Rect(0, 0, _whiteLogo.width, _whiteLogo.width),
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

    public static GuildSelector Make(
        GuildSelectorFlowCoordinator guildSelectorFlowCoordinator,
        GuildSaberCache guildSaberCache,
        GuildSaberManager guildSaberManager,
        GuildSaberClient client,
        Texture2D downArrowTexture,
        Texture2D whiteArrowTexture) => new(guildSelectorFlowCoordinator, downArrowTexture, whiteArrowTexture,
        guildSaberCache, guildSaberManager, client);

    public void UpdateGuildButtons()
    {
        if (_guildSaberData.GuildsExtended.Any())
            Guild1.SetGuild(_guildSaberData.GuildsExtended.First().Value.Guild.Id);
        else
            Guild1.SetActive(false);

        if (_guildSaberData.GuildsExtended.Count >= 2)
            Guild2.SetGuild(_guildSaberData.GuildsExtended.ElementAt(1).Value.Guild.Id);
        else
            Guild2.SetActive(false);
    }

    private void OnIconGuildSelected(GuildId guildId)
    {
        var guild = _guildSaberData.GuildsExtended[guildId];
        if (guild == null) return;

        _guildSaberManager.SelectGuild(guildId, guild.Contexts[0].Id);
    }

    private void OnArrowButtonClicked() => _guildSelectorFlowCoordinator.Show(OnGuildSelected);

    public GuildSelector SetOnGuildSelected(Action<GuildResponses.GuildExtended> callback)
    {
        OnGuildSelected = callback;
        return this;
    }

    public GuildSelector Bind(ref GuildSelector x)
    {
        x = this;
        return this;
    }
}