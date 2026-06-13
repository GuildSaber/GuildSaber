using System;
using GuildSaber.Api.Features.Guilds.Http;
using GuildSaber.Mod.Features.Common.UI;
using HMUI;
using Zenject;

namespace GuildSaber.Mod.Features.PlayerCard.UI.Components.GuildSelector;

public class GuildSelectorFlowCoordinator : SimpleFlowCoordinator
{
    [Inject] private readonly GuildSelectorViewController _mainView = null!;

    protected Action<GuildResponses.GuildExtended>? DismissCallback;

    protected override string Title => "Select guild";
    protected override ViewController GetMainViewController() => _mainView;

    public void Show(Action<GuildResponses.GuildExtended> callback)
    {
        DismissCallback = callback;
        if (IsPresent) return;

        _mainView.guildSelectorFlowCoordinator = this;
        Present();
    }

    public void Dismiss(GuildResponses.GuildExtended guildExtended)
    {
        DismissCallback?.Invoke(guildExtended);
        Dismiss();
    }
}