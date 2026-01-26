using GuildSaber.CSharpClient;
using SiraUtil.Logging;
using Zenject;

namespace GuildSaber.Mod.Installers;

public class PlayerCardInstaller(GuildSaberClient client, SiraLog logger) : Installer
{
    public override void InstallBindings() => logger.Info(client.HttpClient.BaseAddress);
}