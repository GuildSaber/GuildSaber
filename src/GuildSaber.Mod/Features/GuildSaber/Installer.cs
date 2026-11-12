using System;
using GuildSaber.CSharpClient;
using GuildSaber.Mod.Features.GuildSaber.Caching;
using GuildSaber.Mod.Features.GuildSaber.Runtime;
using Zenject;

namespace GuildSaber.Mod.Features.GuildSaber;

public class GuildSaberInstaller(GuildSaberConfig config) : Installer
{
    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<GuildSaberClientProvider>().AsSingle();
        Container.Bind<Func<GuildSaberClient>>()
            .FromMethod(context => context.Container.Resolve<GuildSaberClientProvider>().GetClient)
            .AsSingle();

        Container.BindInterfacesAndSelfTo<GuildSaberManager>().AsSingle();
        Container.BindInterfacesAndSelfTo<GuildAssetCache>().AsSingle();
        Container.BindInterfacesAndSelfTo<GuildSaberCacheStore>().AsSingle();

        Container.BindInstance(config);
    }
}