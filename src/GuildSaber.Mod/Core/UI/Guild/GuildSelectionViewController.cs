using System.Collections.Generic;
using System.Linq;
using CP_SDK_BS.UI;
using CP_SDK.XUI;
using GuildSaber.Api.Features.Guilds;
using GuildSaber.Mod.Core.UI.Guild.Components;
using Zenject;

namespace GuildSaber.Mod.Core.UI.Guild;

internal class GuildSelectionViewController : ViewController<GuildSelectionViewController>
{
    protected XUIVScrollView _guildListContainer = null!;

    protected readonly List<GuildButton> _guildButtons = new List<GuildButton>();

    [Inject] private readonly ModData _modData = null!;
    [Inject] private readonly GuildSelectionFlowCoordinator _guildSelectionFlowCoordinator = null!;
    
    protected override void OnViewCreation()
    {
        XUIVLayout.Make(
                XUIHLayout.Make(
                        XUIVScrollView.Make()
                            .Bind(ref _guildListContainer)
                    )
                    .SetHeight(80)
                    .OnReady(x => x.CSizeFitter.verticalFit = x.CSizeFitter.horizontalFit =
                        UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained)
                    .OnReady(x => x.HOrVLayoutGroup.childForceExpandHeight =
                        x.HOrVLayoutGroup.childForceExpandWidth = true)
            )
            .SetWidth(100)
            .SetHeight(80)
            .SetBackground(true)
            .SetBackgroundColor(new UnityEngine.Color(0f, 0f, 0f, 0f))
            .BuildUI(RTransform);
    }

    ///////////////////////////////////////////////////////
    //////////////////////////////////////////////////////

    protected override void OnViewActivation()
    {
        if (_guildButtons.Any()) return;

        foreach (var x in _modData.Guilds)
        {
            //if (x == null) continue;

            var l_Button = GuildButton.Make();
            l_Button.SetWidth(70);
            l_Button.SetHeight(10);
            l_Button.OnClicked += OnGuildButtonClicked;
            _guildButtons.Add(l_Button);
            l_Button.BuildUI(_guildListContainer.Element.Container);
            l_Button.SetGuild(x);
        }
    }

    ///////////////////////////////////////////////////////
    //////////////////////////////////////////////////////

    private void OnGuildButtonClicked(GuildResponses.Guild x)
    {
        _guildSelectionFlowCoordinator.Dismiss(x);
    }
}