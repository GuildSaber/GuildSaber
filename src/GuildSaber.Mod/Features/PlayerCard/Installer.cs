using BeatSaberMarkupLanguage.FloatingScreen;
using GuildSaber.CSharpClient;
using GuildSaber.Mod.Features.PlayerCard.Patches;
using GuildSaber.Mod.Features.PlayerCard.UI;
using GuildSaber.Mod.Features.PlayerCard.UI.Components;
using GuildSaber.Mod.Resources;
using HMUI;
using UnityEngine;
using Zenject;

namespace GuildSaber.Mod.Features.PlayerCard;

public class PlayerCardInstaller(GuildSaberClient client, Logger logger) : Installer
{
    internal class PlayerCardResourcesFactory(
        [Inject(Id = ResourceMap.DownArrow)] Texture2D downArrowTexture,
        [Inject(Id = ResourceMap.GsWhiteLogo)] Texture2D gsWhiteLogoTexture,
        [Inject(Id = ResourceMap.PlasticTrophy)] Texture2D plasticTrophy,
        [Inject(Id = ResourceMap.SilverTrophy)] Texture2D silverTrophy,
        [Inject(Id = ResourceMap.GoldTrophy)] Texture2D goldTrophy,
        [Inject(Id = ResourceMap.DiamondTrophy)] Texture2D diamondTrophy,
        [Inject(Id = ResourceMap.RubyTrophy)] Texture2D rubyTrophy,
        [Inject] StandardLevelDetailView standardLevelDetailView) : IFactory<PlayerCardResources>
    {
        public PlayerCardResources Create() => new(
            BorderSprite: standardLevelDetailView.actionButton.transform
                .Find("Border").GetComponent<ImageView>().sprite,
            BorderMaterial: standardLevelDetailView.actionButton.transform
                .Find("BG").GetComponent<ImageView>().material,
            DownArrowTexture: downArrowTexture,
            GsWhiteLogoTexture: gsWhiteLogoTexture,
            PlasticTrophyTexture: plasticTrophy,
            SilverTrophyTexture: silverTrophy,
            GoldTrophyTexture: goldTrophy,
            DiamondTrophyTexture: diamondTrophy,
            RubyTrophyTexture: rubyTrophy
        );
    }

    public override void InstallBindings()
    {
        logger.Info($"Client base api uri: {client.HttpClient.BaseAddress}");
        logger.Info($"Client user agent: {client.HttpClient.DefaultRequestHeaders.UserAgent}");

        Container.Bind<PlayerCardResources>().FromFactory<PlayerCardResourcesFactory>().AsSingle();
        Container.BindInterfacesAndSelfTo<PlayerCardPlayTime>().AsSingle();
        Container.Bind<FloatingScreen>()
            .WithId(Constants.CardFloatingPanelId)
            .FromMethod
            (() => FloatingScreen.CreateFloatingScreen(
                screenSize: new Vector2(50, 20),
                createHandle: false,
                position: Vector3.zero,
                rotation: Quaternion.Euler(Vector3.zero)))
            .AsSingle();
        Container.BindInterfacesAndSelfTo<PlayerCardView>().FromNewComponentAsViewController().AsSingle();
        Container.Bind<PlayerCardSettingsView>().FromNewComponentAsViewController().AsSingle();
        Container.Bind<PlayerCardSettings>().FromNewComponentOnNewGameObject().AsSingle();
        Container.Bind<StandardLevelDetailView>().FromComponentInHierarchy().AsCached();
        Container.BindInterfacesAndSelfTo<PlayerCardManager>().AsSingle();

        Container.Bind<PlayerCardGuildPickerView>().FromNewComponentAsViewController().AsSingle();
        Container.Bind<PlayerCardGuildPicker>().FromNewComponentOnNewGameObject().AsSingle();

        Container.BindInterfacesAndSelfTo<GameReloadAffinityPatch>().AsSingle();
        Container.BindInterfacesAndSelfTo<PauseAffinityPatch>().AsSingle();
    }
}