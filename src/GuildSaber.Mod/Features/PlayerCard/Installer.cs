using BeatSaberMarkupLanguage.FloatingScreen;
using GuildSaber.CSharpClient;
using GuildSaber.Mod.Features.PlayerCard.Patches;
using GuildSaber.Mod.Features.PlayerCard.UI;
using GuildSaber.Mod.Features.PlayerCard.UI.Components.GuildSelector;
using GuildSaber.Mod.Features.PlayerCard.UI.Settings;
using GuildSaber.Mod.Resources;
using HMUI;
using TMPro;
using UnityEngine;
using Zenject;

namespace GuildSaber.Mod.Features.PlayerCard;

public class PlayerCardInstaller(GuildSaberClient client, Logger logger) : Installer
{
    internal class PlayerCardResourcesFactory(
        [Inject(Id = nameof(ResourceMap.DownArrow))] Texture2D downArrowTexture,
        [Inject(Id = nameof(ResourceMap.GsWhiteLogo))] Texture2D gsWhiteLogoTexture,
        [Inject(Id = nameof(ResourceMap.PlasticTrophy))] Texture2D plasticTrophy,
        [Inject(Id = nameof(ResourceMap.SilverTrophy))] Texture2D silverTrophy,
        [Inject(Id = nameof(ResourceMap.GoldTrophy))] Texture2D goldTrophy,
        [Inject(Id = nameof(ResourceMap.DiamondTrophy))] Texture2D diamondTrophy,
        [Inject(Id = nameof(ResourceMap.RubyTrophy))] Texture2D rubyTrophy,
        [Inject(Id = nameof(ResourceMap.TekoMedium))] TMP_FontAsset font,
        [Inject] StandardLevelDetailView standardLevelDetailView) : IFactory<PlayerCardResources>
    {
        public PlayerCardResources Create() => new(
            BorderSprite: standardLevelDetailView.actionButton.transform
                .Find("Border").GetComponent<ImageView>().sprite,
            BorderMaterial: standardLevelDetailView.actionButton.transform
                .Find("BG").GetComponent<ImageView>().material,
            DownArrowTexture: downArrowTexture,
            GsWhiteLogoTexture: gsWhiteLogoTexture,
            Font: font,
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
        Container.Bind<StandardLevelDetailView>().FromComponentInHierarchy().AsCached();

        Container.Bind<GuildSelectorViewController>().FromNewComponentAsViewController().AsSingle();
        Container.Bind<GuildSelectorFlowCoordinator>().FromNewComponentOnNewGameObject().AsSingle();

        Container.BindInterfacesAndSelfTo<GameReloadAffinityPatch>().AsSingle();
    }
}