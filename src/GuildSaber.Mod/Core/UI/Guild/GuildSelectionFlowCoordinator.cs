using System;
using GuildSaber.Api.Features.Guilds;
using GuildSaber.Mod.Core.UI.Common;
using HMUI;
using Zenject;

namespace GuildSaber.Mod.Core.UI.Guild;

public class GuildSelectionFlowCoordinator : CustomFlowCoordinator
{
    [Inject] private readonly GuildSelectionViewController _mainView = null!;
    [Inject] private readonly GuildSaberManager _guildSaberManager = null!;
    
    protected Action<GuildResponses.GuildExtended>? DismissCallback;

    protected override string Title => "Select guild";
    protected override ViewController? GetMainViewController() => _mainView;

    public void Show(Action<GuildResponses.GuildExtended> callback)
    {
        DismissCallback = callback;
        if (IsPresent) return;
        
        _mainView.GuildSelectionFlowCoordinator = this;
        Present();
    }

    public void Dismiss(GuildResponses.GuildExtended guildExtended)
    {
        _guildSaberManager.SetGuild(guildExtended);
        DismissCallback?.Invoke(guildExtended);
        Dismiss();
    }
}