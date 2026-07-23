using System.Collections.Generic;
using CP_SDK_BS.UI;
using CP_SDK.XUI;
using GuildSaber.Mod.Features.Common.UI;
using GuildSaber.Mod.Features.Common.UI.Components;
using GuildSaber.Mod.Features.GuildSaber.Runtime;
using GuildSaber.Mod.Features.MenuTweaks.RankedMapStats;
using GuildSaber.Mod.Features.PlayerCard.UI;
using Zenject;

namespace GuildSaber.Mod.Features.GuildSaber.Settings;

public class GuildSaberSettingsView : ViewController<GuildSaberSettingsView>
{
    /// <remarks>
    /// Order must be retained as it is used to set the dropdown value based on the enum index.
    /// </remarks>
    private readonly List<string> _apiEnvironments = [nameof(ApiEnv.Prod), nameof(ApiEnv.Dev)];

    [Inject] private readonly GuildSaberConfig _config = null!;
    [Inject] private readonly GuildSaberManager _guildSaberManager = null!;
    [Inject] private readonly PlayerCardSettings _playerCardSettings = null!;
    [Inject] private readonly RankedMapStats _rankedMapStats = null!;
    [Inject] private readonly UIFactory _uiFactory = null!;

    private GSDropdown _apiDropdown = null!;
    private XUIToggle _displayMapRankedStatsToggle = null!;

    protected override void OnViewCreation() => Templates.FullRectLayoutMainView(
            _uiFactory.Text("Api environment:"),
            _uiFactory.Dropdown()
                .Bind(ref _apiDropdown)
                .SetOptions(_apiEnvironments)
                .OnValueChanged(OnApiEnvChanged),
            _uiFactory.Text("Display RankedMap Stats:"),
            XUIToggle.Make()
                .OnValueChanged(OnDisplayMapRankedStatsChanged)
                .Bind(ref _displayMapRankedStatsToggle),
            _uiFactory.SecondaryButton("Player card settings")
                .SetWidth(40)
                .SetHeight(5)
                .OnClick(_playerCardSettings.Present))
        .OnReady(_ => UpdateValues())
        .BuildUI(transform);

    public void UpdateValues()
    {
        _displayMapRankedStatsToggle.SetValue(_config.RankedMapStats.Enabled, false);
        _apiDropdown.SetValue(_apiEnvironments[(int)_config.ApiEnv]);
    }

    private void OnApiEnvChanged(int index, string value)
    {
        _config.ApiEnv = (ApiEnv)index;
        _ = _guildSaberManager.SelectGuildAsync(_config.GuildId, _config.ContextId);
    }

    private void OnDisplayMapRankedStatsChanged(bool value)
    {
        _config.RankedMapStats.Enabled = value;
        _rankedMapStats.SetActive(value);
    }
}