using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CP_SDK.UI.Components;
using CP_SDK.XUI;
using GuildSaber.Api.Features.Guilds;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Mod.Core.UI.Guild;
using GuildSaber.Mod.Core.UI.Utils;
using UnityEngine;

namespace GuildSaber.Mod.Core.PlayerCard.UI.Components;

public class GuildSelector : XUIHLayout
{
    private readonly Texture2D _downArrowTexture;
    private readonly ModData _guildSaberData;
    private readonly GuildSaberManager _guildSaberManager;
    private readonly GuildSelectionFlowCoordinator _guildSelectionFlowCoordinator;
    private readonly Texture2D _whiteLogoTexture;

    protected XUIIconButton ArrowButton = null!;

    ///////////////////////////////////////////////////////
    //////////////////////////////////////////////////////

    protected GuildIconButton Guild1 = null!;
    protected GuildIconButton Guild2 = null!;

    protected Action<GuildResponses.GuildExtended> OnGuildSelected = null!;

    public GuildSelector(
        GuildSelectorParams selectorParams)
        : base("GuildSelector")
    {
        _whiteLogoTexture = selectorParams.WhiteArrowTexture;
        _downArrowTexture = selectorParams.DownArrowTexture;
        _guildSelectionFlowCoordinator = selectorParams.GuildSelectionFlowCoordinator;
        _guildSaberData = selectorParams.ModData;
        _guildSaberManager = selectorParams.GuildSaberManager;
        OnReady(OnCreation);
    }


    public readonly record struct GuildSelectorParams(
        GuildSelectionFlowCoordinator GuildSelectionFlowCoordinator,
        Texture2D DownArrowTexture,
        Texture2D WhiteArrowTexture,
        ModData ModData,
        GuildSaberManager GuildSaberManager
    );


    public class GuildIconButton : XUIIconButton
    {
        private readonly ModData _guildSaberData;
        private readonly List<Action<GuildId>> _onGuildSelected = [];
        private readonly Texture2D _whiteLogo;

        //////////////////////////////////////////////////////
        /////////////////////////////////////////////////////

        private GuildId _guildID;

        public GuildIconButton(ModData guildSaberData, Texture2D whiteLogo, Action<GuildId>? onGuildSelected) :
            base("GuildIconButton", null)
        {
            _whiteLogo = whiteLogo;

            var sprite = Sprite.Create(_whiteLogo,
                new Rect(0, 0, _whiteLogo.width, _whiteLogo.width),
                Vector2.zero);

            _guildSaberData = guildSaberData;
            SetSprite(sprite);
            OnClick(OnButtonClicked);
            if (onGuildSelected != null) OnGuildSelected(onGuildSelected);
        }

        public static GuildIconButton
            Make(ModData guildSaberData, Texture2D whiteLogo, Action<GuildId>? onGuildSelected) =>
            new(guildSaberData, whiteLogo, onGuildSelected);


        //////////////////////////////////////////////////////
        /////////////////////////////////////////////////////

        public async void SetGuild(GuildId guildId)
        {
            SetActive(true);

            _guildID = guildId;

            var guildLogo = await _guildSaberData.GetGuildLogo(guildId);
            if (guildLogo == null)
                return;

            await TextureUtils.RoundTextureAsync(guildLogo, guildLogo.width * 0.1f);

            SetSprite(Sprite.Create(guildLogo, new Rect(0, 0, guildLogo.width, guildLogo.height), Vector2.zero));
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

    // R:26 G:28 B:30

    public static GuildSelector Make(GuildSelectorParams param) => new(param);

    protected void OnCreation(CHLayout x)
    {
// Je vais manger. Je re après. 
// mais je suppose que ici t'auras un truc qui fait que t'arrête de passer toutes les data (une boucle sur les guilds), juste le logo et le guildId que tu veux non?
// Bah je me sers du data parce que j'avais prévu de pouvoir changer la guilde du bouton à certains moment, ce sera galère sans les fonctions de la data
// Bon app sinon
// ća me parait comme un drôle de comportement X), perso jdirais même que il connais rien à part l'icone a afficher et lui il trigger juste l'action que t'as bind avec le bon id, et pis c'est tout
        Guild1 = new GuildIconButton(_guildSaberData, _whiteLogoTexture, OnIconGuildSelected);
        Guild2 = new GuildIconButton(_guildSaberData, _whiteLogoTexture, OnIconGuildSelected);

        Make(
            Guild1,
            Guild2,
            XUIIconButton.Make()
                .Bind(ref ArrowButton)
                .SetSprite(Sprite.Create(_downArrowTexture,
                    new Rect(0, 0, _downArrowTexture.width, _downArrowTexture.height),
                    Vector2.zero))
                .OnClick(OnArrowButtonClicked)
                .SetWidth(10)
                .SetHeight(10)
                .OnReady(x => x.transform.localRotation = Quaternion.Euler(0, 0, 90))
        ).BuildUI(Element.transform);

        SetBackgroundColor(new Color(26.0f / 255, 28.0f / 255, 30.0f / 255));

        try
        {
            Guild1.SetGuild(_guildSaberData.Guilds.ElementAt(0).Guild.Id);
            Guild2.SetGuild(_guildSaberData.Guilds.ElementAt(1).Guild.Id);
        }
        catch
        {
            // ignored
        }
    }

    public void UpdateGuildButtons()
    {
        if (_guildSaberData.Guilds.Any())
            Guild1.SetGuild(_guildSaberData.Guilds[0].Guild.Id);
        else
            Guild1.SetActive(false);

        if (_guildSaberData.Guilds.Count >= 2)
            Guild2.SetGuild(_guildSaberData.Guilds[1].Guild.Id);
        else
            Guild2.SetActive(false);
    }

    private void OnIconGuildSelected(GuildId guildId)
    {
        var guild = _guildSaberData.GetGuild(guildId);
        if (guild == null) return;

        _guildSaberManager.SelectGuild(guildId, guild.Contexts[0].Id);
    }

    private void OnArrowButtonClicked() => _guildSelectionFlowCoordinator.Show(OnGuildSelected);

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