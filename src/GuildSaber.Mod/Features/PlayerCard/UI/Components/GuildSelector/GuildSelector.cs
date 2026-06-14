using System;
using CP_SDK.XUI;
using GuildSaber.Api.Features.Guilds.Http;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Mod.Features.GuildSaber.Caching;
using GuildSaber.Mod.Features.GuildSaber.Runtime;
using UnityEngine;

namespace GuildSaber.Mod.Features.PlayerCard.UI.Components.GuildSelector;

public class GuildSelector : XUIHLayout
{
    private readonly GuildAssetCache _guildAssetCache;
    private readonly GuildSelectorFlowCoordinator _guildSelectorFlowCoordinator;
    private readonly GuildSaberSession _session;

    protected XUIIconButton ArrowButton = null!;

    ///////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////

    protected GuildIconButton Guild1 = null!;
    protected GuildIconButton Guild2 = null!;

    protected Action<GuildResponses.GuildExtended> OnGuildSelected = _ => { };

    public GuildSelector(
        GuildSelectorFlowCoordinator guildSelectorFlowCoordinator,
        Texture2D downArrowTexture,
        Texture2D whiteArrowTexture,
        GuildSaberSession session,
        GuildAssetCache guildAssetCache)
        : base("GuildSelector")
    {
        var whiteLogoTexture = whiteArrowTexture;
        var downArrowTexture1 = downArrowTexture;
        _guildSelectorFlowCoordinator = guildSelectorFlowCoordinator;
        _session = session;
        _guildAssetCache = guildAssetCache;

        OnReady(element =>
        {
            Guild1 = new GuildIconButton(_guildAssetCache, whiteLogoTexture, OnIconGuildSelected);
            Guild2 = new GuildIconButton(_guildAssetCache, whiteLogoTexture, OnIconGuildSelected);

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
            ).BuildUI(element.transform);

            SetBackgroundColor(new Color(26.0f / 255, 28.0f / 255, 30.0f / 255));

            try
            {
                if (_session.TryGetAvailableGuildAt(0, out var firstGuild))
                    Guild1.SetGuild(firstGuild.Guild.Id);

                if (_session.TryGetAvailableGuildAt(1, out var secondGuild))
                    Guild2.SetGuild(secondGuild.Guild.Id);
            }
            catch
            {
                // ignored
            }
        });
    }

    public static GuildSelector Make(
        GuildSelectorFlowCoordinator guildSelectorFlowCoordinator,
        GuildSaberSession session,
        GuildAssetCache guildAssetCache,
        Texture2D downArrowTexture,
        Texture2D whiteArrowTexture)
        => new(guildSelectorFlowCoordinator, downArrowTexture, whiteArrowTexture, session, guildAssetCache);

    public void UpdateGuildButtons()
    {
        if (_session.TryGetAvailableGuildAt(0, out var firstGuild))
            Guild1.SetGuild(firstGuild.Guild.Id);
        else
            Guild1.SetActive(false);

        if (_session.TryGetAvailableGuildAt(1, out var secondGuild))
            Guild2.SetGuild(secondGuild.Guild.Id);
        else
            Guild2.SetActive(false);
    }

    private void OnIconGuildSelected(GuildId guildId)
    {
        if (!_session.TryGetGuild(guildId, out var guild))
            return;

        OnGuildSelected.Invoke(guild);
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