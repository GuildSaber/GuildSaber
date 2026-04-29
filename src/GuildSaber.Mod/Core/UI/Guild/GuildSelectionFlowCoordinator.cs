using System;
using GuildSaber.Api.Features.Guilds;
using GuildSaber.Mod.Core.UI.Common;
using HMUI;
using Zenject;

namespace GuildSaber.Mod.Core.UI.Guild;

public class GuildSelectionFlowCoordinator : CustomFlowCoordinator
{
    [Inject] private readonly GuildSelectionViewController _mainView = null!;
    
    protected override string Title => "Select guild";
    protected override ViewController? GetMainViewController() => _mainView;

    protected Action<GuildResponses.Guild>? DismissCallback;
    
    public void Show(Action<GuildResponses.Guild> callback)
    {
        DismissCallback = callback;
    }

    public void Dismiss(GuildResponses.Guild guildId)
    {
        DismissCallback?.Invoke(guildId);
    }
    
}