using System.Reflection;

namespace GuildSaber.Api.Endpoints.Internal;

public static class EndPointExtensions
{
    public static void AddEndpoints<TMarker>(this IServiceCollection services, IConfiguration configuration)
        => AddEndpoints(services, configuration, typeof(TMarker));

    public static void MapEndpoints<TMarker>(this IApplicationBuilder app)
        => MapEndpoints(app, typeof(TMarker));

    public static void AddEndpoints(IServiceCollection services, IConfiguration configuration, Type typeMarker)
    {
        var endpointsTypes = GetEndpointsTypesFromAssemblyContaining(typeMarker);

        foreach (var type in endpointsTypes)
            type.GetMethod(nameof(IEndPoints.AddServices))
                ?.Invoke(null, [services, configuration]);
    }

    public static void MapEndpoints(IApplicationBuilder app, Type typeMarker)
    {
        var endpointsTypes = GetEndpointsTypesFromAssemblyContaining(typeMarker);

        foreach (var type in endpointsTypes)
            type.GetMethod(nameof(IEndPoints.MapEndpoints))
                ?.Invoke(null, [app]);
    }

    private static IEnumerable<TypeInfo> GetEndpointsTypesFromAssemblyContaining(Type typeMarker)
        => typeMarker.Assembly.DefinedTypes
            .Where(x => x is { IsAbstract: false, IsInterface: false }
                        && typeof(IEndPoints).IsAssignableFrom(x)
            );
}