using GuildSaber.Mod.Core.PlayerCard.UI;
using GuildSaber.Mod.Core.UI.Common;
using HMUI;
using Zenject;

namespace GuildSaber.Mod.Core.PlayerCard;

public class PlayerCardSettingsCoordinator : CustomFlowCoordinator
{
    [Inject] private readonly PlayerCardSettingsMainView _mainView = null!;


    protected override string Title => "Player card settings";

    protected override ViewController? GetMainViewController() => _mainView;
}