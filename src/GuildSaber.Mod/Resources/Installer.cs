using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using Zenject;
using IPALogger = IPA.Logging.Logger;

namespace GuildSaber.Mod.Resources;

public class ResourcesInstaller(IPALogger logger) : Installer
{
    public override void InstallBindings()
    {
        Container.Bind<Texture2D>().WithId(ResourceMap.DownArrow)
            .FromMethod(() => LoadTexture2DFromResource(ResourceMap.DownArrow, logger))
            .AsCached();

        Container.Bind<Texture2D>().WithId(ResourceMap.GsWhiteLogo)
            .FromMethod(() => LoadTexture2DFromResource(ResourceMap.GsWhiteLogo, logger))
            .AsCached();

        Container.Bind<Texture2D>().WithId(ResourceMap.PlasticTrophy)
            .FromMethod(() => LoadTexture2DFromResource(ResourceMap.PlasticTrophy, logger))
            .AsCached();

        Container.Bind<Texture2D>().WithId(ResourceMap.SilverTrophy)
            .FromMethod(() => LoadTexture2DFromResource(ResourceMap.SilverTrophy, logger))
            .AsCached();

        Container.Bind<Texture2D>().WithId(ResourceMap.GoldTrophy)
            .FromMethod(() => LoadTexture2DFromResource(ResourceMap.GoldTrophy, logger))
            .AsCached();

        Container.Bind<Texture2D>().WithId(ResourceMap.DiamondTrophy)
            .FromMethod(() => LoadTexture2DFromResource(ResourceMap.DiamondTrophy, logger))
            .AsCached();

        Container.Bind<Texture2D>().WithId(ResourceMap.RubyTrophy)
            .FromMethod(() => LoadTexture2DFromResource(ResourceMap.RubyTrophy, logger))
            .AsCached();

        Container.Bind<Texture2D>().WithId(ResourceMap.CheckMark)
            .FromMethod(() => LoadTexture2DFromResource(ResourceMap.CheckMark, logger))
            .AsCached();

        Container.Bind<Texture2D>().WithId(ResourceMap.DenyMark)
            .FromMethod(() => LoadTexture2DFromResource(ResourceMap.DenyMark, logger))
            .AsCached();

        Container.Bind<Texture2D>().WithId(ResourceMap.QuestionMark)
            .FromMethod(() => LoadTexture2DFromResource(ResourceMap.QuestionMark, logger))
            .AsCached();

        Container.Bind<Texture2D>().WithId(ResourceMap.CheckShield)
            .FromMethod(() => LoadTexture2DFromResource(ResourceMap.CheckShield, logger))
            .AsCached();

        Container.Bind<Texture2D>().WithId(ResourceMap.DenyShield)
            .FromMethod(() => LoadTexture2DFromResource(ResourceMap.DenyShield, logger))
            .AsCached();

        Container.Bind<TMP_FontAsset>().WithId(ResourceMap.TekoMedium)
            .FromMethod(() =>
            {
                var font = UnityEngine.Resources
                    .FindObjectsOfTypeAll<TextMeshProUGUI>()
                    .First(x => x.font.name.Contains(ResourceMap.TekoMedium) && x.font.name.Contains("Curved"))
                    .font;

                logger.Debug(
                    $"[{nameof(ResourcesInstaller)}/TMP_FontAsset] Loaded {nameof(ResourceMap.TekoMedium)}: {font.name}"
                );

                return font;
            }).AsCached();
    }

    public static Texture2D LoadTexture2DFromResource(string resourcePath, IPALogger logger)
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