using GuildSaber.Database.Contexts.Server;

namespace GuildSaber.Api.Features.Players.Pipelines;

public static class PlayerPointsPipeline
{
    public static ValueTask RecalculatePlayerPoints(PlayerId playerId, ServerDbContext dbContext)
        => throw new NotImplementedException();
}