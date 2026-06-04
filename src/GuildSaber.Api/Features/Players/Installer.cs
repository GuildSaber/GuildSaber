using GuildSaber.Api.Features.Players.Pipelines;

namespace GuildSaber.Api.Features.Players;

public static class PlayersInstaller
{
    public static IServiceCollection AddPlayersFeature(this IServiceCollection services) => services
        .AddTransient<PlayerScoresPipeline>();
}