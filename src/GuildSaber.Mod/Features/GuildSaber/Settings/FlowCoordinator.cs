using GuildSaber.Mod.Features.Common.UI;
using HMUI;
using Zenject;

namespace GuildSaber.Mod.Features.GuildSaber.Settings;

public class GuildSaberSettingsFlowCoordinator : SimpleFlowCoordinator
{
    [Inject] private readonly GuildSaberSettingsView _view = null!;
    
    protected override string Title => "Guild Saber Settings";

    protected override ViewController? GetMainViewController()
        => _view;
}