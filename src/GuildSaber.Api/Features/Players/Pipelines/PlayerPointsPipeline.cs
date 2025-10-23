using GuildSaber.Database.Contexts.Server;

namespace GuildSaber.Api.Features.Players.Pipelines;

public static class PlayerPointsPipeline
{
    public static ValueTask RecalculatePlayerPoints(PlayerId playerId, ServerDbContext dbContext)
        => ValueTask.CompletedTask; //throw new NotImplementedException();
}