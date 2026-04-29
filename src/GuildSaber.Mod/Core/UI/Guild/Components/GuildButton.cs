using System;
using CP_SDK.UI.Components;
using CP_SDK.XUI;
using GuildSaber.Api.Features.Guilds;
using GuildSaber.Mod.Core.UI.Common;
using GuildSaber.Mod.Core.UI.Extensions;
using GuildSaber.Mod.PlayerCard.UI.Components;
using UnityEngine;

namespace GuildSaber.Mod.Core.UI.Guild.Components;

internal class GuildButton : GSSecondaryButton
    {
        public override Color GetColor()
        {
            return Color.black.ColorWithAlpha(0.7f);
        }
        
        protected GuildButton(string label, Action? onClick = null) : base(label, onClick)
        {
            OnReady(OnCreation);
            OnClick(OnGuildSelected);
        }

        public static GuildButton Make()
        {
            return new GuildButton(string.Empty);
        }

        public event Action<GuildResponses.Guild> OnClicked = null!;

        ///////////////////////////////////////////////////////
        //////////////////////////////////////////////////////

        protected GuildSelector.GuildIconButton guildIcon = null!;
        protected GSText _guildName = null!;

        protected GuildResponses.Guild _currentGuild = null!;

        protected void OnCreation(CSecondaryButton x)
        {
            guildIcon = GuildSelector.GuildIconButton.Make();

            _guildName = GSText.Make(string.Empty);
            _guildName.SetMargins(0, 1, 0, 0);
            _guildName.OnReady(x => x.LElement.ignoreLayout = true);
            _guildName.OnReady(x => x.RTransform.sizeDelta = Vector2.zero);
            _guildName.OnReady(x => x.RTransform.anchorMin = Vector2.zero);
            _guildName.OnReady(x => x.RTransform.anchorMax = new Vector2(1, 1));

            XUIHLayout.Make(
                XUIVLayout.Make(
                    guildIcon
                ).SetMinWidth(8),
                XUIHLayout.Make(
                    _guildName
                ).SetMinWidth(50) 
            )
             .SetHeight(10)
             .SetMinHeight(8)
             .BuildUI(Element.LElement.transform);
        }

        ///////////////////////////////////////////////////////
        //////////////////////////////////////////////////////
        
        public void SetGuild(GuildResponses.Guild? guild)
        {
            if (guild == null)
            {
                SetActive(false);
            }
            else
            {
                guildIcon.SetGuild(guild.Id);

                string shortName = guild.Info.Name.Substring(0, 18);

                if (guild.Info.Name.Length != shortName.Length)
                {
                    shortName += "...";
                }
                
                _guildName.SetText(shortName);
                _currentGuild = guild;
            }
        }

        private void OnGuildSelected()
        {
            OnClicked.Invoke(_currentGuild);
        }
    }
