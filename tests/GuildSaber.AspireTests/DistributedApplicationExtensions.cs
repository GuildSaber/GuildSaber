using System.Security.Cryptography;

namespace GuildSaber.AspireTests;

public static class DistributedApplicationExtensions
{
    /// <summary>
    /// Sets the container lifetime for all container resources in the application.
    /// </summary>
    public static TBuilder WithContainersLifetime<TBuilder>(this TBuilder builder, ContainerLifetime containerLifetime)
        where TBuilder : IDistributedApplicationTestingBuilder
    {
        var containerLifetimeAnnotations = builder.Resources.SelectMany(r => r.Annotations
                .OfType<ContainerLifetimeAnnotation>()
                .Where(c => c.Lifetime != containerLifetime))
            .ToList();

        foreach (var annotation in containerLifetimeAnnotations)
            annotation.Lifetime = containerLifetime;

        return builder;
    }

    public static void RemoveResources<TResource>(this IDistributedApplicationTestingBuilder builder)
        where TResource : ContainerResource
    {
        var resources = builder.Resources
            .OfType<TResource>()
            .ToList();

        foreach (var resource in resources) builder.Resources.Remove(resource);
    }


    /// <summary>
    /// Replaces all named volumes with anonymous volumes so they're isolated across test runs and from the volume the app uses
    /// during development.
    /// </summary>
    /// <remarks>
    /// Note that if multiple resources share a volume, the volume will instead be given a random name so that it's still
    /// shared across those resources in the test run.
    /// </remarks>
    public static TBuilder WithRandomVolumeNames<TBuilder>(this TBuilder builder)
        where TBuilder : IDistributedApplicationTestingBuilder
    {
        /* Named volumes that aren't shared across resources should be replaced with anonymous volumes.
         * Named volumes shared by multiple resources need to have their name randomized but kept shared
         * across those resources. */
        var seenVolumes = new HashSet<string>();
        var renamedVolumes = new Dictionary<string, string>();

        // Find all shared volumes and make a map of their original name to a new randomized name.
        var allResourceNamedVolumes = builder.Resources.SelectMany(r => r.Annotations
                .OfType<ContainerMountAnnotation>()
                .Where(m => m.Type == ContainerMountType.Volume && !string.IsNullOrEmpty(m.Source))
                .Select(m => (Resource: r, Volume: m)))
            .ToList();

        foreach (var name in allResourceNamedVolumes
                     .Select(resourceVolume => resourceVolume.Volume.Source!)
                     .Where(name => !seenVolumes.Add(name) && !renamedVolumes.ContainsKey(name)))
            renamedVolumes[name] = $"{name}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(4))}";

        foreach (var (resource, volume) in allResourceNamedVolumes)
        {
            var newMount = new ContainerMountAnnotation(
                source: renamedVolumes.GetValueOrDefault(volume.Source!),
                target: volume.Target,
                type: ContainerMountType.Volume,
                isReadOnly: volume.IsReadOnly
            );

            resource.Annotations.Remove(volume);
            resource.Annotations.Add(newMount);
        }

        return builder;
    }
}