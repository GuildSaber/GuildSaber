using System;
using CP_SDK.XUI;
using GuildSaber.Api.Features.Guilds;
using GuildSaber.CSharpClient;
using GuildSaber.Mod.Features.Common.UI;
using GuildSaber.Mod.Features.Common.UI.Components;
using GuildSaber.Mod.Features.GuildSaber;
using GuildSaber.Mod.Features.PlayerCard.UI.Components.GuildSelector;
using GuildSaber.Mod.Helpers;
using TMPro;
using UnityEngine;

namespace GuildSaber.Mod.Features.PlayerCard.UI.Components;

public class GuildButton : GSSecondaryButton
{
    private readonly GuildIconButton _guildIcon;
    private readonly GSText _guildName;
    private GuildResponses.GuildExtended _currentGuild = null!;

    public GuildButton(
        GuildSaberCache guildSaberData, UIFactory uiFactory, Texture2D guildSaberWhiteLogo, TMP_FontAsset font,
        GuildSaberClient client, Action? onClick = null) : base(string.Empty, font, onClick)
    {
        _guildIcon = GuildIconButton.Make(guildSaberData, guildSaberWhiteLogo, client, null);
        _guildName = uiFactory.Text("");

        OnReady(element =>
        {
            _guildName.SetMargins(0, 1, 0, 0);
            _guildName.OnReady(x => x.LElement.ignoreLayout = true);
            _guildName.OnReady(x => x.RTransform.sizeDelta = Vector2.zero);
            _guildName.OnReady(x => x.RTransform.anchorMin = Vector2.zero);
            _guildName.OnReady(x => x.RTransform.anchorMax = new Vector2(1, 1));

            XUIHLayout.Make(
                    XUIVLayout.Make(_guildIcon)
                        .SetMinWidth(8),
                    XUIHLayout.Make(_guildName)
                        .SetMinWidth(50)
                )
                .SetHeight(10)
                .SetMinHeight(8)
                .BuildUI(element.transform);
        });

        OnClick(OnGuildSelected);
    }

    public event Action<GuildResponses.GuildExtended> OnClicked = null!;

    public static GuildButton Make(
        GuildSaberCache guildSaberData, UIFactory factory, Texture2D guildSaberWhiteLogo, TMP_FontAsset font,
        GuildSaberClient client) => new(guildSaberData, factory, guildSaberWhiteLogo, font, client);

    public override Color GetColor() => Color.black.ColorWithAlpha(0.7f);

    ///////////////////////////////////////////////////////
    //////////////////////////////////////////////////////

    public void SetGuild(GuildResponses.GuildExtended? guild)
    {
        if (guild == null)
        {
            SetActive(false);
            return;
        }

        _guildIcon.SetGuild(guild.Guild.Id);

        var shortName = guild.Guild.Info.Name switch
        {
            { Length: > 32 } val => val[..30] + "...",
            var val => val
        };

        _guildName.SetText(shortName);
        _currentGuild = guild;
    }

    private void OnGuildSelected() => OnClicked.Invoke(_currentGuild);
}