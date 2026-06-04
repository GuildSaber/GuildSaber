using System.Collections.Generic;
using System.Linq;
using CP_SDK_BS.UI;
using CP_SDK.XUI;
using GuildSaber.Api.Features.Guilds.Http;
using GuildSaber.CSharpClient;
using GuildSaber.Mod.Features.Common.UI;
using GuildSaber.Mod.Features.GuildSaber;
using GuildSaber.Mod.Resources;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace GuildSaber.Mod.Features.PlayerCard.UI.Components.GuildSelector;

internal class GuildSelectorViewController : ViewController<GuildSelectorViewController>
{
    public GuildSelectorFlowCoordinator guildSelectorFlowCoordinator = null!;

    [Inject] private readonly GuildSaberClient _client = null!;
    [Inject(Id = nameof(ResourceMap.TekoMedium))] private readonly TMP_FontAsset _font = null!;
    protected readonly List<GuildButton> _guildButtons = new();

    [Inject] private readonly GuildSaberCache _guildSaberCache = null!;

    [Inject] private readonly UIFactory _uiFactory = null!;

    [Inject(Id = nameof(ResourceMap.GsWhiteLogo))]
    private readonly Texture2D _whiteLogoTexture = null!;


    protected XUIVScrollView _guildListContainer = null!;

    protected override void OnViewCreation() => XUIVLayout.Make(
            XUIHLayout.Make(
                    XUIVScrollView.Make()
                        .Bind(ref _guildListContainer)
                )
                .SetHeight(80)
                .OnReady(x => x.CSizeFitter.verticalFit = x.CSizeFitter.horizontalFit =
                    ContentSizeFitter.FitMode.Unconstrained)
                .OnReady(x => x.HOrVLayoutGroup.childForceExpandHeight =
                    x.HOrVLayoutGroup.childForceExpandWidth = true)
        )
        .SetWidth(100)
        .SetHeight(80)
        .SetBackground(true)
        .SetBackgroundColor(new Color(0f, 0f, 0f, 0f))
        .BuildUI(RTransform);

    ///////////////////////////////////////////////////////
    //////////////////////////////////////////////////////

    protected override void OnViewActivation()
    {
        if (_guildButtons.Any()) return;

        foreach (var (_, guildExtended) in _guildSaberCache.GuildsExtended)
        {
            var button = GuildButton.Make(_guildSaberCache, _uiFactory, _whiteLogoTexture, _font, _client);
            button.OnClicked += OnGuildButtonClicked;
            button.SetWidth(70);
            button.SetHeight(10);

            _guildButtons.Add(button);
            button.BuildUI(_guildListContainer.Element.Container);
            button.SetGuild(guildExtended);
        }
    }

    ///////////////////////////////////////////////////////
    //////////////////////////////////////////////////////

    private void OnGuildButtonClicked(GuildResponses.GuildExtended x) => guildSelectorFlowCoordinator.Dismiss(x);
}