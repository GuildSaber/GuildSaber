using CP_SDK.UI;
using GuildSaber.Mod.Features.Common.UI;
using GuildSaber.Mod.Features.PlayerCard.UI;
using Zenject;

namespace GuildSaber.Mod.Features.GuildSaber.Settings;

public class GuildSaberSettingsInstaller : Installer
{
    public override void InstallBindings() => Container
        .Bind<GuildSaberSettingsView>()
        .FromFactory<GuildSaberSettingsFactory>()
        .AsSingle()
        .NonLazy();
}

public class GuildSaberSettingsFactory(
    [Inject] PlayerCardView cardView,
    [Inject] GuildSaberConfig config,
    [Inject] GuildSaberManager guildSaberManager,
    [Inject] UIFactory uiFactory,
    [Inject] RankedMapStats.RankedMapStats rankedMapStats) : IFactory<GuildSaberSettingsView>
{
    public GuildSaberSettingsView Create()
    {
        var view = UISystem.CreateViewController<GuildSaberSettingsView>();
        view.Inject(cardView, config, guildSaberManager, uiFactory, rankedMapStats);
        Module.SettingsView = view;
        return view;
    }
}