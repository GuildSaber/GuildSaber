using GuildSaber.Database.Contexts.Server;

namespace GuildSaber.Api.Features.Players.Pipelines;

public static class PlayerLevelPipeline
{
    public static ValueTask RecalculatePlayerLevels(PlayerId playerId, ServerDbContext dbContext)
        => throw new NotImplementedException();
}