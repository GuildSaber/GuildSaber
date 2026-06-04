namespace GuildSaber.Api.Features.Guilds;

public static class GuildsInstaller
{
    public static IServiceCollection AddGuildsFeature(this IServiceCollection services) => services
        .AddScoped<GuildService>();
}