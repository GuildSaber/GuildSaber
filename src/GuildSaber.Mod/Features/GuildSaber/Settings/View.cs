using System.Collections.Generic;
using CP_SDK.UI;
using CP_SDK.XUI;
using GuildSaber.Mod.Features.Common.UI;
using GuildSaber.Mod.Features.Common.UI.Components;
using GuildSaber.Mod.Features.PlayerCard;
using GuildSaber.Mod.Features.PlayerCard.UI;

namespace GuildSaber.Mod.Features.GuildSaber.Settings;

public class GuildSaberSettingsView : ViewController<GuildSaberSettingsView>
{
    /// <remarks>
    /// Order must be retained as it is used to set the dropdown value based on the enum index.
    /// </remarks>
    private readonly List<string> _apiEnvironments = [nameof(ApiEnv.Prod), nameof(ApiEnv.Dev)];

    private GSDropdown _apiDropdown = null!;
    private GuildSaberConfig _config = null!;
    private XUIToggle _displayMapRankedStatsToggle = null!;
    private GuildSaberManager _guildSaberManager = null!;

    private XUIVLayout _mainLayout = null!;

    private PlayerCardView _playerCardView = null!;
    private RankedMapStats.RankedMapStats _rankedMapStats = null!;
    private UIFactory _uiFactory = null!;

    public void Inject(
        PlayerCardView cardView, GuildSaberConfig config, GuildSaberManager guildSaberManager, UIFactory uiFactory,
        RankedMapStats.RankedMapStats rankedMapStats)
    {
        _playerCardView = cardView;
        _config = config;
        _guildSaberManager = guildSaberManager;
        _uiFactory = uiFactory;
        _rankedMapStats = rankedMapStats;

        CreateUI();
    }

    private void CreateUI() => _mainLayout = Templates.FullRectLayoutMainView(
        _uiFactory.Text("Api environment:"),
        _uiFactory.Dropdown()
            .Bind(ref _apiDropdown)
            .SetOptions(_apiEnvironments)
            .OnValueChanged(OnApiEnvChanged),
        _uiFactory.Text("Display map ranked stat:"),
        XUIToggle.Make()
            .OnValueChanged(OnDisplayMapRankedStatsChanged)
            .Bind(ref _displayMapRankedStatsToggle),
        _uiFactory.SecondaryButton("Reset card position")
            .SetWidth(40)
            .SetHeight(5)
            .OnClick(ResetCardPosition)
    ).OnReady(_ => UpdateValues());

    protected override void OnViewCreation() => _mainLayout.BuildUI(transform);

    public void UpdateValues()
    {
        _displayMapRankedStatsToggle.SetValue(_config.RankedMapStats.Enabled, false);
        _apiDropdown.SetValue(_apiEnvironments[(int)_config.ApiEnv]);
    }

    private void OnApiEnvChanged(int index, string value)
    {
        _config.ApiEnv = (ApiEnv)index;
        _guildSaberManager.SelectGuild(_config.GuildId, _config.ContextId);
    }

    private void OnDisplayMapRankedStatsChanged(bool value)
    {
        _config.RankedMapStats.Enabled = value;
        _rankedMapStats.SetActive(value);
    }

    private void ResetCardPosition()
    {
        _config.PlayerCard.Transforms.Menu = new PlayerCardConfig().Transforms.Menu;
        _playerCardView.SetCardToMenuTransform();
    }
}