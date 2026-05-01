using System;
using GuildSaber.Api.Features.Guilds;
using GuildSaber.Mod.Core.UI.Common;
using HMUI;
using Zenject;

namespace GuildSaber.Mod.Core.UI.Guild;

public class GuildSelectionFlowCoordinator : CustomFlowCoordinator
{
    [Inject] private readonly GuildSelectionViewController _mainView = null!;

    protected Action<GuildResponses.GuildExtended>? DismissCallback;

    protected override string Title => "Select guild";
    protected override ViewController? GetMainViewController() => _mainView;

    public void Show(Action<GuildResponses.GuildExtended> callback)
    {
        DismissCallback = callback;
        _mainView.GuildSelectionFlowCoordinator = this;
        Present();
    }

    public void Dismiss(GuildResponses.GuildExtended guildExtended) => DismissCallback?.Invoke(guildExtended);
}