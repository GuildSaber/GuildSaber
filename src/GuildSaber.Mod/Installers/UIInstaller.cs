using System.Linq;
using GuildSaber.CSharpClient;
using GuildSaber.Mod.Configurations;
using GuildSaber.Mod.Core;
using GuildSaber.Mod.Core.UI;
using GuildSaber.Mod.Core.UI.RankedMap;
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
            mapRankedStat.RTransform.offsetMin = new Vector2(75, -4);
            var rectTransform = levelParamsPanel.GetComponent<RectTransform>();
            rectTransform.offsetMax += new Vector2(0, 0);

            standardLevelDetailViewController.didChangeContentEvent += mapRankedStat.BeatmapContentChanged;
            standardLevelDetailView.didChangeDifficultyBeatmapEvent += mapRankedStat.BeatmapDifficultyChanged;

            return mapRankedStat;
        }
    }

    public override void InstallBindings()
        => Container.Bind<MapRankedStat>().FromFactory<MapRankedStatFactory>().AsSingle().NonLazy();
}