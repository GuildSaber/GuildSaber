using System.Collections.Generic;
using System.Linq;
using CP_SDK_BS.UI;
using CP_SDK.XUI;
using GuildSaber.Api.Features.Guilds;
using GuildSaber.Mod.Core.UI.Guild.Components;
using GuildSaber.Mod.Resources;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace GuildSaber.Mod.Core.UI.Guild;

internal class GuildSelectionViewController : ViewController<GuildSelectionViewController>
{
    protected readonly List<GuildButton> _guildButtons = new();
    public GuildSelectionFlowCoordinator GuildSelectionFlowCoordinator = null!;

    [Inject] private readonly ModData _modData = null!;

    [Inject(Id = nameof(ResourceMap.GsWhiteLogo))]
   private readonly Texture2D _whiteLogoTexture = null!;

    [Inject] private readonly UIFactory _uiFactory = null!;
    [Inject(Id = nameof(ResourceMap.TekoMedium))] private readonly TMP_FontAsset _tekoFont = null!;


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

        foreach (var x in _modData.Guilds)
        {
            //if (x == null) continue;

            var button = GuildButton.Make(_modData, _uiFactory, _whiteLogoTexture, _tekoFont);
            button.SetWidth(70);
            button.SetHeight(10);
            button.OnClicked += OnGuildButtonClicked;
            _guildButtons.Add(button);
            button.BuildUI(_guildListContainer.Element.Container);
            button.SetGuild(x);
        }
    }

    ///////////////////////////////////////////////////////
    //////////////////////////////////////////////////////

    private void OnGuildButtonClicked(GuildResponses.GuildExtended x) => GuildSelectionFlowCoordinator.Dismiss(x);
}