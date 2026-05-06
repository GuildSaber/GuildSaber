using GuildSaber.Mod.Features.Common.UI.Components;
using HMUI;
using Zenject;

namespace GuildSaber.Mod.Features.PlayerCard.UI.Settings;

public class PlayerCardSettingsCoordinator : CustomFlowCoordinator
{
    [Inject] private readonly PlayerCardSettingsMainView _mainView = null!;

    protected override string Title => "Player card settings";
    protected override ViewController GetMainViewController() => _mainView;
}