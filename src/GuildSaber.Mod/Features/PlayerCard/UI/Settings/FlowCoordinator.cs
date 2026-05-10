using GuildSaber.Mod.Features.Common.UI;
using HMUI;
using Zenject;

namespace GuildSaber.Mod.Features.PlayerCard.UI.Settings;

public class PlayerCardSettingsCoordinator : SimpleFlowCoordinator
{
    [Inject] private readonly PlayerCardSettingsMainView _mainView = null!;

    protected override string Title => "Player card settings";
    protected override ViewController GetMainViewController() => _mainView;
}