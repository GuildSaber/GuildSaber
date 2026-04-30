using System;
using System.Collections.Generic;
using System.Linq;
using CP_SDK.UI.Components;
using CP_SDK.XUI;
using GuildSaber.Api.Features.Guilds;
using GuildSaber.Mod.Core.UI.Guild;
using GuildSaber.Mod.Core.UI.Utils;
using UnityEngine;
using Zenject;

namespace GuildSaber.Mod.Core.PlayerCard.UI.Components;

internal class GuildSelector : XUIHLayout
{
    [Inject] private readonly PlayerCardResources _cardResources = null!;
    [Inject] private readonly GuildSelectionFlowCoordinator _guildSelectionFlowCoordinator = null!;

    [Inject] private readonly ModData _modData = null!;

    protected XUIIconButton ArrowButton = null!;

    ///////////////////////////////////////////////////////
    //////////////////////////////////////////////////////

    protected GuildIconButton Guild1 = null!;
    protected GuildIconButton Guild2 = null!;
    protected GuildIconButton Guild3 = null!;

    protected Action<GuildResponses.Guild> OnGuildSelected = null!;

    protected GuildSelector(string name, params IXUIElement[] childs) : base(name, childs) => OnReady(OnCreation);

    internal class GuildIconButton : XUIIconButton
    {
        [Inject] private readonly ModData _modData = null!;

        //////////////////////////////////////////////////////
        /////////////////////////////////////////////////////

        private readonly List<Action<int>> _onGuildSelected = [];
        [Inject] private readonly PlayerCardResources _resources = null!;

        //////////////////////////////////////////////////////
        /////////////////////////////////////////////////////

        private int _guildID;


        protected GuildIconButton(string name, Action? onClick = null) : base(name, null, onClick)
        {
            var sprite = Sprite.Create(_resources.GsWhiteLogoTexture,
                new Rect(0, 0, _resources.GsWhiteLogoTexture.width, _resources.GsWhiteLogoTexture.width),
                Vector2.zero);

            SetSprite(sprite);
            OnClick(OnButtonClicked);
        }


        //////////////////////////////////////////////////////
        /////////////////////////////////////////////////////

        public static GuildIconButton Make() => new("GuildIcon");

        public async void SetGuild(int guildId)
        {
            _guildID = guildId;

            var guildLogo = _modData.GetGuildLogo(guildId);
            if (guildLogo == null)
                return;

            var roundedLogo = await TextureUtils.CreateRoundedTextureAsync(guildLogo, guildLogo.width * 0.1f);

            SetSprite(Sprite.Create(roundedLogo, new Rect(0, 0, roundedLogo.width, roundedLogo.height), Vector2.zero));
            SetWidth(8);
            SetHeight(8);
        }

        //////////////////////////////////////////////////////
        /////////////////////////////////////////////////////

        public GuildIconButton OnClick(Action<int> x)
        {
            _onGuildSelected.Add(x);
            return this;
        }

        private void OnButtonClicked()
        {
            foreach (var l_Item in _onGuildSelected) l_Item.Invoke(_guildID);
        }
    }

    // R:26 G:28 B:30

    public static GuildSelector Make() => new("GuildSelector");

    protected void OnCreation(CHLayout x)
    {
        Guild1 = GuildIconButton.Make();
        Guild2 = GuildIconButton.Make();
        Guild3 = GuildIconButton.Make();

        Make(
            Guild1.OnClick(OnIconGuildSelected),
            Guild2.OnClick(OnIconGuildSelected),
            Guild3.OnClick(OnIconGuildSelected),
            XUIIconButton.Make()
                .Bind(ref ArrowButton)
                .SetSprite(Sprite.Create(_cardResources.DownArrowTexture,
                    new Rect(0, 0, _cardResources.DownArrowTexture.width, _cardResources.DownArrowTexture.height),
                    Vector2.zero))
                .OnClick(OnArrowButtonClicked)
                .SetWidth(10)
                .SetHeight(10)
                .OnReady(x => x.transform.localRotation = Quaternion.Euler(0, 0, 90))
        ).BuildUI(Element.transform);

        SetBackgroundColor(new Color(26.0f / 255, 28.0f / 255, 30.0f / 255));

        try
        {
            Guild1.SetGuild(_modData.Guilds.ElementAt(0).Id.Value);
            Guild2.SetGuild(_modData.Guilds.ElementAt(1).Id.Value);
            Guild3.SetGuild(_modData.Guilds.ElementAt(2).Id.Value);
        }
        catch
        {
            // ignored
        }
    }

    private void OnIconGuildSelected(int guildID)
    {
        var guild = _modData.GetGuild(guildID);
        if (guild is not null) OnGuildSelected.Invoke(guild);
    }

    private void OnArrowButtonClicked() => _guildSelectionFlowCoordinator.Show(OnGuildSelected);

    public GuildSelector SetOnGuildSelected(Action<GuildResponses.Guild> callback)
    {
        OnGuildSelected = callback;
        return this;
    }
}