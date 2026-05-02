using GuildSaber.Mod.Configurations;
using GuildSaber.Mod.Installers;
using HarmonyLib;
using IPA;
using IPA.Config.Stores;
using ModestTree;
using SiraUtil.Zenject;
using IPALogger = IPA.Logging.Logger;
using IPAConfig = IPA.Config.Config;

namespace GuildSaber.Mod;

[Plugin(RuntimeOptions.SingleStartInit)]
public class Plugin
{
    private readonly HarmonyLib.Harmony _guildSaberHarmony = new HarmonyLib.Harmony("guildsaber.mod.sheepvand");
    
    [Init]
    public Plugin(Zenjector zenjector, IPALogger logger, IPAConfig config)
    {
        zenjector.UseLogger(logger);
        var pluginConfig = config.Generated<PluginConfig>();

        zenjector.Install<CoreInstaller>(Location.App);
        zenjector.Install<AppInstaller>(Location.App, pluginConfig);
        zenjector.Install<ResourcesInstaller>(Location.App, logger);
        zenjector.Install<UIInstaller>(Location.App);
        zenjector.Install<PlayerCardInstaller>(Location.Menu);
    }

    [OnEnable]
    public void OnEnable()
    {
        _guildSaberHarmony.PatchAll();
    }
        

    [OnDisable]
    public void OnDisable()
    {
        _guildSaberHarmony.UnpatchSelf();
    }
}