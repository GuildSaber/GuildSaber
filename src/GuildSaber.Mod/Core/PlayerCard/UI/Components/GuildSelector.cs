using System;
using System.Collections.Generic;
using System.Linq;
using CP_SDK.UI.Components;
using CP_SDK.XUI;
using GuildSaber.Api.Features.Guilds;
using GuildSaber.Mod.Core;
using GuildSaber.Mod.Core.PlayerCard;
using GuildSaber.Mod.Core.UI.Utils;
using UnityEngine;
using Zenject;

namespace GuildSaber.Mod.PlayerCard.UI.Components;

    internal class GuildSelector : XUIHLayout
    {

        internal class GuildIconButton : XUIIconButton
        {

            [Inject] private readonly PlayerCardResources _resources = null!;
            [Inject] private readonly ModData _modData = null!;
            
            protected GuildIconButton(string name, Action? onClick = null) : base(name, null, onClick)
            {
                var l_Sprite = Sprite.Create(_resources.GsWhiteLogoTexture,
                    new Rect(0, 0, _resources.GsWhiteLogoTexture.width,
                        _resources.GsWhiteLogoTexture.width), Vector2.zero);

                SetSprite(l_Sprite);
                OnClick(OnButtonClicked);
            }


            //////////////////////////////////////////////////////
            /////////////////////////////////////////////////////

            public static GuildIconButton Make()
            {
                return new GuildIconButton("GuildIcon");
            }

            //////////////////////////////////////////////////////
            /////////////////////////////////////////////////////

            int _guildID;

            public async void SetGuild(int guildId)
            {
                var l_GuildLogo = _modData.GetGuildLogo(guildId);
                if (l_GuildLogo == null) return;

                _guildID = guildId;

                Texture2D l_RoundedLogo = await TextureUtils.CreateRoundedTexture(l_GuildLogo, l_GuildLogo.width * 0.1f);
                SetSprite(Sprite.Create(l_RoundedLogo, new Rect(0, 0, l_RoundedLogo.width, l_RoundedLogo.height), Vector2.zero));

                SetWidth(8);
                SetHeight(8);
            }

            //////////////////////////////////////////////////////
            /////////////////////////////////////////////////////

            List<Action<int>> OnGuildSelected = new List<Action<int>>();

            //////////////////////////////////////////////////////
            /////////////////////////////////////////////////////
            
            public GuildIconButton OnClick(Action<int> x)
            {
                OnGuildSelected.Add(x);
                return this;
            }

            private void OnButtonClicked()
            {
                foreach (var l_Item in OnGuildSelected)
                {
                    l_Item.Invoke(_guildID);
                }
            }

        }

        // R:26 G:28 B:30

        public static GuildSelector Make()
        {
            return new GuildSelector("GuildSelector");
        }

        protected GuildSelector(string name, params IXUIElement[] childs) : base(name, childs)
        {
            OnReady(OnCreation);
        }

        ///////////////////////////////////////////////////////
        //////////////////////////////////////////////////////

        protected GuildIconButton Guild1 = null!;
        protected GuildIconButton Guild2 = null!;
        protected GuildIconButton Guild3 = null!;

        protected XUIIconButton ArrowButton = null!;

        protected Action<GuildResponses.Guild> OnGuildSelected = null!;

        [Inject] private readonly ModData _modData = null!;
        [Inject] private readonly PlayerCardResources _cardResources = null!;
        
        protected void OnCreation(CHLayout x)
        {
            Guild1 = GuildIconButton.Make();
            Guild2 = GuildIconButton.Make();
            Guild3 = GuildIconButton.Make();

            XUIHLayout.Make(
                Guild1.OnClick(OnIconGuildSelected),
                Guild2.OnClick(OnIconGuildSelected),
                Guild3.OnClick(OnIconGuildSelected),
                XUIIconButton.Make()
                    .Bind(ref ArrowButton)
                    .SetSprite(Sprite.Create(_cardResources.DownArrowTexture, new Rect(0, 0, _cardResources.DownArrowTexture.width, _cardResources.DownArrowTexture.height), Vector2.zero))
                    .OnClick(OnArrowButtonClicked)
                    .SetWidth(10)
                    .SetHeight(10)
                    .OnReady(x => x.transform.localRotation = Quaternion.Euler(0, 0, 90))
            ).BuildUI(Element.transform);

            SetBackgroundColor(new Color(26.0f/255, 28.0f/255, 30.0f/255));

            try
            {
                Guild1.SetGuild(_modData.Guilds.ElementAt(0).Id.Value);
                Guild2.SetGuild(_modData.Guilds.ElementAt(1).Id.Value);
                Guild3.SetGuild(_modData.Guilds.ElementAt(2).Id.Value);
            } catch
            {

            }
        }

        private void OnIconGuildSelected(int guildID)
        {
            var l_Guild = _modData.GetGuild(guildID);
            if (l_Guild != null)
                OnGuildSelected.Invoke(l_Guild);
        }

        private void OnArrowButtonClicked()
        {
            GuildSelectionFlowCoordinator.Instance.SetCallback(OnGuildSelected);
            GuildSelectionFlowCoordinator.Instance.Present();
        }

        public GuildSelector SetOnGuildSelected(Action<GuildResponses.Guild> callback)
        {
            OnGuildSelected = callback;
            return this;
        }

    }