using System.Linq;
using BeatSaberMarkupLanguage.FloatingScreen;
using GuildSaber.CSharpClient;
using GuildSaber.Mod.Core.PlayerCard;
using GuildSaber.Mod.Core.PlayerCard.UI;
using GuildSaber.Mod.Core.PlayerCard.UI.Settings;
using GuildSaber.Mod.Core.Time;
using GuildSaber.Mod.Resources;
using HMUI;
using SiraUtil.Logging;
using TMPro;
using UnityEngine;
using Zenject;

namespace GuildSaber.Mod.Installers;

public class PlayerCardInstaller(GuildSaberClient client, SiraLog logger) : Installer
{
    internal class PlayerCardResourcesFactory(
        [Inject(Id = nameof(ResourceMap.DownArrow))] Texture2D downArrowTexture,
        [Inject(Id = nameof(ResourceMap.GsWhiteLogo))] Texture2D gsWhiteLogoTexture
    )
        : IFactory<PlayerCardResources>
    {
        public PlayerCardResources Create()
        {
            var standardLevelDetailView = UnityEngine.Resources.FindObjectsOfTypeAll<StandardLevelDetailView>().First();

            var resources = new GameObject("PlayerCardResources").AddComponent<PlayerCardResources>();
            resources.SetValues(
                borderSprite: standardLevelDetailView.actionButton.transform
                    .Find("Border").GetComponent<ImageView>().sprite,
                borderMaterial: standardLevelDetailView.actionButton.transform
                    .Find("BG").GetComponent<ImageView>().material,
                downArrowTexture: downArrowTexture,
                gsWhiteLogoTexture: gsWhiteLogoTexture,
                tekoFont: UnityEngine.Resources.FindObjectsOfTypeAll<TextMeshProUGUI>()
                    .Where(x => x.font.name.Contains("Teko-Medium")).ElementAt(1).font
            );

            return resources;
        }
    }

    public override void InstallBindings()
    {
        logger.Info($"Client base api uri: {client.HttpClient.BaseAddress}");
        logger.Info($"Client user agent: {client.HttpClient.DefaultRequestHeaders.UserAgent}");

        Container.Bind<PlayerCardResources>().FromFactory<PlayerCardResourcesFactory>().AsSingle();
        Container.Bind<TimeController>().FromNewComponentOnNewGameObject().AsSingle();
        Container.Bind<FloatingScreen>()
            .WithId(Constants.CardFloatingPanelId)
            .FromMethod
            (() => FloatingScreen.CreateFloatingScreen(
                screenSize: new Vector2(50, 20),
                createHandle: false,
                position: Vector3.zero,
                rotation: Quaternion.Euler(Vector3.zero)))
            .AsSingle();
        Container.Bind<PlayerCardView>().FromNewComponentAsViewController().AsSingle();
        Container.Bind<PlayerCardSettingsMainView>().FromNewComponentAsViewController().AsSingle();
        Container.Bind<PlayerCardSettingsCoordinator>().FromNewComponentOnNewGameObject().AsSingle();
        Container.BindInterfacesTo<PlayerCardManager>().AsSingle();
    }
}