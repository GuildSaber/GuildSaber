using GuildSaber.Api.Features.Guilds.Achievements.Services;
using GuildSaber.Api.Features.Guilds.Categories;

namespace GuildSaber.Api.Features.Guilds;

public static class GuildsInstaller
{
    public static IServiceCollection AddGuildsFeature(this IServiceCollection services) => services
        .AddScoped<GuildService>()
        .AddScoped<AchievementService>()
        .AddSingleton<GuildStorage>()
        .AddSingleton<AchievementStorage>()
        .AddSingleton<CategoryStorage>();
}