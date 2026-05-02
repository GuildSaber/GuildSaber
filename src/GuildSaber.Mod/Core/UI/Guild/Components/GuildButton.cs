using System;
using CP_SDK.UI.Components;
using CP_SDK.XUI;
using GuildSaber.Api.Features.Guilds;
using GuildSaber.Mod.Core.PlayerCard.UI.Components;
using GuildSaber.Mod.Core.UI.Common;
using GuildSaber.Mod.Core.UI.Extensions;
using TMPro;
using UnityEngine;

namespace GuildSaber.Mod.Core.UI.Guild.Components;

public class GuildButton : GSSecondaryButton
{
    protected GuildResponses.GuildExtended _currentGuild = null!;
    protected GSText _guildName = null!;
    private readonly ModData _guildSaberData;
    private readonly UIFactory _uiFactory;
    private readonly Texture2D _guildSaberWhiteLogo;

    ///////////////////////////////////////////////////////
    //////////////////////////////////////////////////////

    protected GuildSelector.GuildIconButton guildIcon = null!;

    public static GuildButton Make(ModData guildSaberData, UIFactory factory, Texture2D guildSaberWhiteLogo,
        TMP_FontAsset font)
        => new(guildSaberData, factory, guildSaberWhiteLogo, font);

    public GuildButton(ModData guildSaberData, UIFactory uiFactory, Texture2D guildSaberWhiteLogo, TMP_FontAsset font,
        Action? onClick = null)
        : base(string.Empty, font, onClick)
    {
        _guildSaberData = guildSaberData;
        _guildSaberWhiteLogo = guildSaberWhiteLogo;
        _uiFactory = uiFactory;
        OnReady(OnCreation);
        OnClick(OnGuildSelected);
    }

    public override Color GetColor() => Color.black.ColorWithAlpha(0.7f);

    public event Action<GuildResponses.GuildExtended> OnClicked = null!;

    protected void OnCreation(CSecondaryButton c)
    {
        guildIcon = GuildSelector.GuildIconButton.Make(_guildSaberData, _guildSaberWhiteLogo, null);

        _guildName = _uiFactory.Text("");
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

    public void SetGuild(GuildResponses.GuildExtended? guild)
    {
        if (guild == null)
        {
            SetActive(false);
            return;
        }

        guildIcon.SetGuild(guild.Guild.Id);

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