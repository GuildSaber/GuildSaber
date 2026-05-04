using System.Linq;
using BeatSaberMarkupLanguage.MenuButtons;
using CP_SDK.UI;
using GuildSaber.CSharpClient;
using GuildSaber.Mod.Configurations;
using GuildSaber.Mod.Core;
using GuildSaber.Mod.Core.PlayerCard.UI;
using GuildSaber.Mod.Core.PlayerCard.UI.Settings;
using GuildSaber.Mod.Core.UI;
using GuildSaber.Mod.Core.UI.RankedMap;
using GuildSaber.Mod.Core.UI.Settings;
using GuildSaber.Mod.Module;
using GuildSaber.Mod.Resources;
using UnityEngine;
using Zenject;

namespace GuildSaber.Mod.Installers;

public class UIInstaller : Installer
{
    public class MapRankedStatFactory(
        [Inject] GuildSaberCache guildSaberCache,
        [Inject] UIFactory uiFactory,
        [Inject] PluginConfig config,
        [Inject] GuildSaberClient client,
        [Inject(Id = nameof(ResourceMap.GsWhiteLogo))] Texture2D gsWhiteLogoTexture
    ) : IFactory<MapRankedStat>
    {
        public MapRankedStat Create()
        {
            var standardLevelDetailView = UnityEngine.Resources
                .FindObjectsOfTypeAll<StandardLevelDetailView>()
                .First();
            var levelParamsPanel = standardLevelDetailView._levelParamsPanel;
            var standardLevelDetailViewController = UnityEngine.Resources
                .FindObjectsOfTypeAll<StandardLevelDetailViewController>()
                .First();

            var mapRankedStat = new MapRankedStat(guildSaberCache, uiFactory, config, client, gsWhiteLogoTexture);
            mapRankedStat.BuildUI(levelParamsPanel.transform);
            mapRankedStat.RTransform.offsetMin = new Vector2(80, -5);
            var rectTransform = levelParamsPanel.GetComponent<RectTransform>();
            rectTransform.offsetMax += new Vector2(0, 0);

            standardLevelDetailViewController.didChangeContentEvent += mapRankedStat.BeatmapContentChanged;
            standardLevelDetailView.didChangeDifficultyBeatmapEvent += mapRankedStat.BeatmapDifficultyChanged;

            return mapRankedStat;
        }
    }

    public class GuildSaberSettingsFactory(
        [Inject] PlayerCardView cardView, 
        [Inject] PluginConfig config,
        [Inject] GuildSaberManager guildSaberManager,
        [Inject] UIFactory uiFactory,
        [Inject] MapRankedStat mapRankedStat) : IFactory<GuildSaberSettingsView>
    {
        public GuildSaberSettingsView Create()
        {
            var view = UISystem.CreateViewController<GuildSaberSettingsView>();
            view.Inject(cardView, config, guildSaberManager, uiFactory, mapRankedStat);
            GSModule.SettingsView = view;
            return view;
        }
    }
    
    public override void InstallBindings()
    {
        Container.Bind<MapRankedStat>().FromFactory<MapRankedStatFactory>().AsSingle().NonLazy();
        Container.Bind<GuildSaberSettingsView>().FromFactory<GuildSaberSettingsFactory>().AsSingle().NonLazy();
    }
}