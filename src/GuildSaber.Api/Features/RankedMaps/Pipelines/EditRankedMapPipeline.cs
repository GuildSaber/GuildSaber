using GuildSaber.Api.Features.PointCurves;
using GuildSaber.Database.Contexts.Server;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.RankedMaps.Pipelines;

public class EditRankedMapPipeline(
    ServerDbContext dbContext,
    MapPointCurvePipeline curvePipeline,
    ILogger<EditRankedMapPipeline> logger)
{
    public async Task ExecuteAsync(
        RankedMapId rankedMapId, bool refreshMemberStats = true, CancellationToken token = default)
    {
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Executing edit pipeline for ranked map with Id {RankedMapId}", rankedMapId);

        var points = await dbContext.RankedMaps
            .Where(x => x.Id == rankedMapId)
            .SelectMany(x => x.Context.Points)
            .Select(x => x.Id)
            .ToArrayAsync(token);

        foreach (var point in points)
            await curvePipeline.ExecuteAsync(
                rankedMapId, point, true, refreshMemberStats: refreshMemberStats, token: token);

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Completed editing ranked map with Id {RankedMapId}", rankedMapId);
    }
}