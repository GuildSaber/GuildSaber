using System.IO;
using System.Reflection;
using GuildSaber.Mod.Resources;
using UnityEngine;
using Zenject;
using Logger = IPA.Logging.Logger;

namespace GuildSaber.Mod.Installers;

public class ResourcesInstaller(Logger logger) : Installer
{
    public override void InstallBindings()
    {
        Container.Bind<Texture2D>().WithId(nameof(ResourceMap.DownArrow))
            .FromMethod(() => LoadTexture2DFromResource(ResourceMap.DownArrow, logger))
            .AsCached();

        Container.Bind<Texture2D>().WithId(nameof(ResourceMap.GsWhiteLogo))
            .FromMethod(() => LoadTexture2DFromResource(ResourceMap.GsWhiteLogo, logger))
            .AsCached();
    }

    private static Texture2D LoadTexture2DFromResource(string resourcePath, Logger logger)
    {
        logger.Debug($"[{nameof(ResourcesInstaller)}/{nameof(LoadTexture2DFromResource)}] " +
                     $"Loading Texture2D from resource: {resourcePath}");

        using var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourcePath);
        using var memoryStream = new MemoryStream();
        var texture = new Texture2D(2, 2);

        if (resourceStream is null)
            throw new FileNotFoundException($"Resource not found: {resourcePath}");

        resourceStream.CopyTo(memoryStream);
        texture.LoadImage(memoryStream.ToArray());

        logger.Debug($"[{nameof(ResourcesInstaller)}/{nameof(LoadTexture2DFromResource)}] " +
                     $"Loaded Texture2D: {resourcePath} ({texture.width}x{texture.height})");

        return texture;
    }
}