using System.Reflection;

namespace GuildSaber.Api.Extensions;

public interface IEndpoints
{
    public static abstract void MapEndpoints(IEndpointRouteBuilder endpoints);
    public static virtual void AddServices(IServiceCollection services, IConfiguration configuration) { }
}

public static class EndpointsExtensions
{
    public static void AddEndpoints<TMarker>(this IServiceCollection services, IConfiguration configuration)
        => AddEndpoints(services, configuration, typeof(TMarker));

    public static void MapEndpoints<TMarker>(this IApplicationBuilder app)
        => MapEndpoints(app, typeof(TMarker));

    private static void AddEndpoints(IServiceCollection services, IConfiguration configuration, Type typeMarker)
    {
        var endpointsTypes = GetEndpointsTypesFromAssemblyContaining(typeMarker);

        foreach (var type in endpointsTypes)
            type.GetMethod(nameof(IEndpoints.AddServices))
                ?.Invoke(null, [services, configuration]);
    }

    private static void MapEndpoints(IApplicationBuilder app, Type typeMarker)
    {
        var endpointsTypes = GetEndpointsTypesFromAssemblyContaining(typeMarker);

        foreach (var type in endpointsTypes)
            type.GetMethod(nameof(IEndpoints.MapEndpoints))
                ?.Invoke(null, [app]);
    }

    private static IEnumerable<TypeInfo> GetEndpointsTypesFromAssemblyContaining(Type typeMarker)
        => typeMarker.Assembly.DefinedTypes
            .Where(x => x is { IsAbstract: false, IsInterface: false } && typeof(IEndpoints).IsAssignableFrom(x));
}