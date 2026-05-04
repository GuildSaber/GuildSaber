using System.Collections.Generic;
using CP_SDK.UI;
using CP_SDK.XUI;
using GuildSaber.Mod.Configurations;
using GuildSaber.Mod.Core.PlayerCard.UI;
using GuildSaber.Mod.Core.PlayerCard.UI.Settings;
using GuildSaber.Mod.Core.UI.Common;
using GuildSaber.Mod.Core.UI.RankedMap;
using UnityEngine.PlayerLoop;
using Zenject;

namespace GuildSaber.Mod.Core.UI.Settings;

public class GuildSaberSettingsView : ViewController<GuildSaberSettingsView>
{
    private readonly List<string> API_ENVS = [ "Prod", "Dev" ];

    private PlayerCardView _playerCardView = null!;
    private PluginConfig _config = null!;
    private GuildSaberManager _guildSaberManager = null!;
    private UIFactory _uiFactory = null!;
    private MapRankedStat _mapRankedStat = null!;

    private XUIVLayout _mainLayout = null!;
    
    private GSDropdown _apiDropdown = null!;
    private XUIToggle _displayMapRankedStatsToggle = null!;

    public void Inject(PlayerCardView cardView, PluginConfig config,
                       GuildSaberManager guildSaberManager, UIFactory uiFactory, MapRankedStat mapRankedStat)
    {
        _playerCardView = cardView;
        _config = config;
        _guildSaberManager = guildSaberManager;
        _uiFactory = uiFactory;
        _mapRankedStat = mapRankedStat;
        
        CreateUI();
    }

    private void CreateUI()
    {
        _mainLayout = Templates.FullRectLayoutMainView(
            _uiFactory.Text("Api environment:"),
            _uiFactory.Dropdown()
                .Bind(ref _apiDropdown)
                .SetOptions(API_ENVS)
                .OnValueChanged(EventApiEnvChanged),
            _uiFactory.Text("Display map ranked stat:"),
            XUIToggle.Make()
                .OnValueChanged(EventChangedDisplayMapRankedStat)
                .Bind(ref _displayMapRankedStatsToggle),
            _uiFactory.SecondaryButton("Reset card position", 40, 5)
                .OnClick(ResetCardPosition)
        ).OnReady(x => UpdateValues());
    }

    protected override void OnViewCreation() => _mainLayout.BuildUI(transform);

    public void UpdateValues()
    {
        _displayMapRankedStatsToggle.SetValue(_config.MapStats.DisplayMapRankedStats, false);
        _apiDropdown.SetValue(API_ENVS[(int)_config.ApiEnv]);
    }
    
    private void EventApiEnvChanged(int index, string value)
    {
        _config.ApiEnv = (ApiEnv)index;
        _guildSaberManager.SelectGuild(_config.PlayerCard.GuildId, _config.PlayerCard.ContextId);
    }
    
    private void EventChangedDisplayMapRankedStat(bool value)
    {
        _config.MapStats.DisplayMapRankedStats = value;
        if (!value)
        {
            _mapRankedStat.SetActive(false);
        }
    }

    private void ResetCardPosition()
    {
        _config.PlayerCard.Transforms.Menu = new CardConfig().Transforms.Menu;
        _playerCardView.SetCardToMenuTransform();
    }
}