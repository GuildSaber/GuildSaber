using GuildSaber.Api.Extensions;
using GuildSaber.Api.Transformers;
using GuildSaber.Database.Contexts.Server;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using static GuildSaber.Api.Features.Scores.Http.ScoreResponses;

namespace GuildSaber.Api.Features.Scores.Http;

public class ScoreEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("scores")
            .WithTag("Scores", description: "Endpoints for accessing scores in the server.");

        group.MapGet("/{scoreId}/statistics", GetScoreStatisticsAsync)
            .WithName("GetScoreStatistics")
            .WithSummary("Get score statistics.")
            .WithDescription("Get score statistics by score Id (BeatLeader kind only).");
    }

    private static async Task<Results<Ok<ScoreStatistics>, NotFound>> GetScoreStatisticsAsync(
        [FromRoute] ScoreId scoreId, ServerDbContext context) => await context.BeatLeaderScores
            .Where(x => x.Id == scoreId)
            .Select(ScoreMappers.MapScoreStatisticsExpression)
            .FirstOrDefaultAsync() switch
        {
            null => TypedResults.NotFound(),
            var x => TypedResults.Ok(x)
        };
}