using BeatSaberMarkupLanguage.FloatingScreen;
using GuildSaber.CSharpClient;
using GuildSaber.Mod.Core.PlayerCard;
using GuildSaber.Mod.Core.PlayerCard.UI;
using GuildSaber.Mod.PlayerCard.UI;
using GuildSaber.Mod.Resources;
using HMUI;
using SiraUtil.Logging;
using UnityEngine;
using Zenject;

namespace GuildSaber.Mod.Installers;

public class PlayerCardInstaller(GuildSaberClient client, SiraLog logger) : Installer
{
    internal class PlayerCardResourcesFactory(
        StandardLevelDetailView standardLevelDetailView,
        [Inject(Id = nameof(ResourceMap.DownArrow))] Texture2D downArrowTexture,
        [Inject(Id = nameof(ResourceMap.GsWhiteLogo))] Texture2D gsWhiteLogoTexture)
        : IFactory<PlayerCardResources>
    {
        public PlayerCardResources Create() => new(
            BorderSprite: standardLevelDetailView.actionButton.transform
                .Find("Border").GetComponent<ImageView>().sprite,
            BorderMaterial: standardLevelDetailView.actionButton.transform
                .Find("BG").GetComponent<ImageView>().material,
            DownArrowTexture: downArrowTexture,
            GsWhiteLogoTexture: gsWhiteLogoTexture
        );
    }

    public override void InstallBindings()
    {
        logger.Info($"Client base api uri: {client.HttpClient.BaseAddress}");
        logger.Info($"Client user agent: {client.HttpClient.DefaultRequestHeaders.UserAgent}");
        
        Container.Bind<PlayerCardResources>().FromFactory<PlayerCardResourcesFactory>().AsSingle();
        Container.Bind<FloatingScreen>()
            .WithId(Constants.CardFloatingPanelId)
            .FromMethod
                (() => FloatingScreen.CreateFloatingScreen(
                    new Vector2(10, 10), 
                    false, 
                    Vector3.zero, 
                    Quaternion.Euler(Vector3.zero)))
            .AsSingle();
        Container.Bind<PlayerCardView>().FromNewComponentAsViewController().AsSingle();
        Container.Bind<PlayerCardSettingsMainView>().FromNewComponentAsViewController().AsSingle();
        Container.Bind<PlayerCardSettingsCoordinator>().FromNewComponentOnNewGameObject().AsSingle();
        Container.BindInterfacesTo<PlayerCardManager>().AsSingle();
    }
}