using GuildSaber.Mod.Features.Common.Timer;
using GuildSaber.Mod.Features.Common.UI;
using GuildSaber.Mod.Features.GuildSaber;
using GuildSaber.Mod.Features.GuildSaber.Settings;
using GuildSaber.Mod.Features.PlayerCard;
using GuildSaber.Mod.Features.PlaylistDownloader;
using GuildSaber.Mod.Features.RankedMapStats;
using GuildSaber.Mod.Resources;
using HarmonyLib;
using IPA;
using IPA.Config.Stores;
using SiraUtil.Zenject;
using IPALogger = IPA.Logging.Logger;
using IPAConfig = IPA.Config.Config;

namespace GuildSaber.Mod;

[Plugin(RuntimeOptions.SingleStartInit)]
public class Plugin
{
    private readonly Harmony _guildSaberHarmony = new("guildsaber.mod.sheepvand");

    [Init]
    public Plugin(Zenjector zenjector, IPALogger logger, IPAConfig config)
    {
        zenjector.UseLogger(logger);
        var pluginConfig = config.Generated<GuildSaberConfig>();

        zenjector.Install<GuildSaberInstaller>(Location.App);
        zenjector.Install<AppInstaller>(Location.App, pluginConfig);
        zenjector.Install<ResourcesInstaller>(Location.App, logger);
        zenjector.Install<UIInstaller>(Location.App);

        zenjector.Install<GuildSaberSettingsInstaller>(Location.Menu);
        zenjector.Install<TimerInstaller>(Location.Menu);
        
        zenjector.Install<PlaylistDownloaderInstaller>(Location.Menu);
        zenjector.Install<PlayerCardInstaller>(Location.Menu);
        zenjector.Install<MapRankedStatsInstaller>(Location.Menu);
    }

    [OnEnable]
    public void OnEnable() => _guildSaberHarmony.PatchAll();


    [OnDisable]
    public void OnDisable() => _guildSaberHarmony.UnpatchSelf();
}