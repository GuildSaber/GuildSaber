using System;
using CP_SDK.XUI;
using GuildSaber.Api.Features.Guilds;
using GuildSaber.CSharpClient;
using GuildSaber.Mod.Features.Common.UI;
using GuildSaber.Mod.Features.Common.UI.Components;
using GuildSaber.Mod.Features.GuildSaber;
using GuildSaber.Mod.Helpers;
using TMPro;
using UnityEngine;

namespace GuildSaber.Mod.Features.PlayerCard.UI.Components;

public class GuildButton : GSSecondaryButton
{
    private readonly GuildSaberClient _client;
    private readonly GuildSaberCache _guildSaberData;
    private readonly Texture2D _guildSaberWhiteLogo;
    private readonly UIFactory _uiFactory;

    protected GuildResponses.GuildExtended _currentGuild = null!;
    protected GSText _guildName = null!;

    ///////////////////////////////////////////////////////
    //////////////////////////////////////////////////////

    protected GuildSelector.GuildSelector.GuildIconButton guildIcon = null!;

    public GuildButton(GuildSaberCache guildSaberData, UIFactory uiFactory, Texture2D guildSaberWhiteLogo,
                       TMP_FontAsset font, GuildSaberClient client, Action? onClick = null)
        : base(string.Empty, font, onClick)
    {
        _guildSaberData = guildSaberData;
        _guildSaberWhiteLogo = guildSaberWhiteLogo;
        _uiFactory = uiFactory;
        _client = client;

        OnReady(_ =>
        {
            guildIcon = GuildSelector.GuildSelector.GuildIconButton.Make(_guildSaberData, _guildSaberWhiteLogo, _client,
                null);

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