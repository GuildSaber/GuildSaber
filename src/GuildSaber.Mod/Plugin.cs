using GuildSaber.Mod.Features.Common.UI;
using GuildSaber.Mod.Features.GuildSaber;
using GuildSaber.Mod.Features.GuildSaber.Settings;
using GuildSaber.Mod.Features.MenuTweaks.PlayButtonRequirements;
using GuildSaber.Mod.Features.MenuTweaks.RankedMapStats;
using GuildSaber.Mod.Features.PlayerCard;
using GuildSaber.Mod.Features.PlaylistDownloader;
using GuildSaber.Mod.Features.RankedMap;
using GuildSaber.Mod.Resources;
using HarmonyLib;
using IPA;
using IPA.Config.Stores;
using SiraUtil.Zenject;
using Zenject;
using IPALogger = IPA.Logging.Logger;
using IPAConfig = IPA.Config.Config;

namespace GuildSaber.Mod;

[Plugin(RuntimeOptions.SingleStartInit)]
public class Plugin
{
    private readonly Harmony _guildSaberHarmony = new("guildsaber.mod");

    [Init]
    public Plugin(Zenjector zenjector, IPALogger logger, IPAConfig ipaConfig)
    {
        zenjector.UseLogger(logger);
        var config = ipaConfig.Generated<GuildSaberConfig>();

        // Resources (Textures, Fonts, etc.) are installed in the static context so they can be used in static factories.
        StaticContext.Container.Install<ResourcesInstaller>([logger]);
        StaticContext.Container.Install<UIInstaller>();

        zenjector.Install<GuildSaberInstaller>(Location.App, config);
        zenjector.Install<GuildSaberSettingsInstaller>(Location.Menu);

        zenjector.Install<PlayerCardInstaller>(Location.Menu);
        zenjector.Install<PlaylistDownloaderInstaller>(Location.Menu);
        zenjector.Install<RankedMapInstaller>(Location.Menu);
        zenjector.Install<MapRankedStatsInstaller>(Location.Menu);
        zenjector.Install<PlayButtonRequirementsInstaller>(Location.Menu);
    }

    [OnEnable]
    public void OnEnable() => _guildSaberHarmony.PatchAll();

    [OnDisable]
    public void OnDisable() => _guildSaberHarmony.UnpatchSelf();
}