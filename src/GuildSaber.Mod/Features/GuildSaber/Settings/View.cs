using System.Collections.Generic;
using CP_SDK.UI;
using CP_SDK.XUI;
using GuildSaber.Mod.Features.Common.UI;
using GuildSaber.Mod.Features.Common.UI.Components;
using GuildSaber.Mod.Features.PlayerCard.UI;
using GuildSaber.Mod.Features.RankedMapStats;

namespace GuildSaber.Mod.Features.GuildSaber.Settings;

public class GuildSaberSettingsView : ViewController<GuildSaberSettingsView>
{
    /// <remarks>
    /// Order must be retained as it is used to set the dropdown value based on the enum index.
    /// </remarks>
    private readonly List<string> _apiEnvironments = [nameof(ApiEnv.Prod), nameof(ApiEnv.Dev)];

    private GSDropdown _apiDropdown = null!;
    private Config _config = null!;
    private XUIToggle _displayMapRankedStatsToggle = null!;
    private GuildSaberManager _guildSaberManager = null!;

    private XUIVLayout _mainLayout = null!;
    private MapRankedStats _mapRankedStats = null!;

    private PlayerCardView _playerCardView = null!;
    private UIFactory _uiFactory = null!;

    public void Inject(
        PlayerCardView cardView, Config config, GuildSaberManager guildSaberManager, UIFactory uiFactory,
        MapRankedStats mapRankedStats)
    {
        _playerCardView = cardView;
        _config = config;
        _guildSaberManager = guildSaberManager;
        _uiFactory = uiFactory;
        _mapRankedStats = mapRankedStats;

        CreateUI();
    }

    private void CreateUI() => _mainLayout = Templates.FullRectLayoutMainView(
        _uiFactory.Text("Api environment:"),
        _uiFactory.Dropdown()
            .Bind(ref _apiDropdown)
            .SetOptions(_apiEnvironments)
            .OnValueChanged(EventApiEnvChanged),
        _uiFactory.Text("Display map ranked stat:"),
        XUIToggle.Make()
            .OnValueChanged(EventChangedDisplayMapRankedStat)
            .Bind(ref _displayMapRankedStatsToggle),
        _uiFactory.SecondaryButton("Reset card position", 40, 5)
            .OnClick(ResetCardPosition)
    ).OnReady(x => UpdateValues());

    protected override void OnViewCreation() => _mainLayout.BuildUI(transform);

    public void UpdateValues()
    {
        _displayMapRankedStatsToggle.SetValue(_config.MapStats.DisplayMapRankedStats, false);
        _apiDropdown.SetValue(_apiEnvironments[(int)_config.ApiEnv]);
    }

    private void EventApiEnvChanged(int index, string value)
    {
        _config.ApiEnv = (ApiEnv)index;
        _guildSaberManager.SelectGuild(_config.PlayerCard.GuildId, _config.PlayerCard.ContextId);
    }

    private void EventChangedDisplayMapRankedStat(bool value)
    {
        _config.MapStats.DisplayMapRankedStats = value;
        if (!value) _mapRankedStats.SetActive(false);
    }

    private void ResetCardPosition()
    {
        _config.PlayerCard.Transforms.Menu = new CardConfig().Transforms.Menu;
        _playerCardView.SetCardToMenuTransform();
    }
}