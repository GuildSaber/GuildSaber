using GuildSaber.Mod.Configurations;
using GuildSaber.Mod.Installers;
using IPA;
using IPA.Config.Stores;
using SiraUtil.Zenject;
using IPALogger = IPA.Logging.Logger;
using IPAConfig = IPA.Config.Config;

namespace GuildSaber.Mod;

[Plugin(RuntimeOptions.SingleStartInit)]
public class Plugin
{
    [Init]
    public Plugin(Zenjector zenjector, IPALogger logger, IPAConfig config)
    {
        zenjector.UseLogger(logger);
        var pluginConfig = config.Generated<PluginConfig>();

        zenjector.Install<AppInstaller>(Location.App, pluginConfig);
        zenjector.Install<PlayerCardInstaller>(Location.Menu | Location.Singleplayer);
    }

    [OnEnable]
    public void OnEnable() { }

    [OnDisable]
    public void OnDisable() { }
}