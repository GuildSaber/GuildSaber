using GuildSaber.CSharpClient;
using GuildSaber.Mod.Configurations;
using GuildSaber.Mod.Extensions;
using Zenject;

namespace GuildSaber.Mod.Installers;

internal class AppInstaller(PluginConfig config) : Installer
{
    public override void InstallBindings()
    {
        Container.BindInstance(config);
        Container.Bind<GuildSaberClient>().FromFactory<GuildSaberClientFactory>().AsSingle();
    }
}

internal class GuildSaberClientFactory(PluginConfig config) : IFactory<GuildSaberClient>
{
    public GuildSaberClient Create() => new(config.ApiEnv.ToApiUri, null);
}