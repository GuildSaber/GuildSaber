namespace GuildSaber.Api.Features.MapRequests;

public static class MapRequestsInstaller
{
    public static IServiceCollection AddMapRequestsFeature(this IServiceCollection services) => services
        .AddScoped<MapProposalService>();
}
