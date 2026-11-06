using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.Auth.Authorization;
using GuildSaber.Api.Extensions;
using GuildSaber.Database.Models.Server.Guilds.Points;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace GuildSaber.Api.Features.PointCurves.Http;

public sealed class PointCurveEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var settings = endpoints.MapGroup("guilds/{guildId}/points/{pointId}/curve-settings")
            .WithTags("PointCurves");

        settings.MapGet("", GetSettingsAsync)
            .WithName("GetPointCurveSettings")
            .Produces<PointCurveResponses.Settings>();

        settings.MapPut("", UpdateSettingsAsync)
            .WithName("UpdatePointCurveSettings")
            .Produces<PointCurveResponses.Settings>()
            .RequireAnyGuildPermission(EPermission.RankingTeam | EPermission.GuildLeader);

        var curves = endpoints.MapGroup("contexts/{contextId}/ranked-maps/{rankedMapId}/points/{pointId}/curve")
            .WithTags("PointCurves");

        curves.MapGet("", GetAsync)
            .WithName("GetMapPointCurve")
            .Produces<PointCurveResponses.Curve>();

        curves.MapPut("", PutAsync)
            .WithName("PutMapPointCurve")
            .Produces<PointCurveResponses.Curve>()
            .RequireAnyGuildPermission(EPermission.RankingTeam | EPermission.GuildLeader);

        curves.MapDelete("", DeleteAsync)
            .WithName("DeleteMapPointCurve")
            .Produces<PointCurveResponses.Curve>()
            .RequireAnyGuildPermission(EPermission.RankingTeam | EPermission.GuildLeader);

        curves.MapPost("/preview", PreviewAsync)
            .WithName("PreviewMapPointCurve")
            .Produces<PointCurveResponses.Preview>();
    }

    private static async Task<IResult> GetSettingsAsync(
        GuildId guildId, int pointId, PointCurveService service, CancellationToken token)
        => Map(await service.GetSettingsAsync(guildId, new Point.PointId(pointId), token));

    private static async Task<IResult> UpdateSettingsAsync(
        GuildId guildId, int pointId, PointCurveRequests.UpdateSettings request,
        PointCurveService service, CancellationToken token)
        => Map(await service.UpdateSettingsAsync(guildId, new Point.PointId(pointId), request, token));

    private static async Task<IResult> GetAsync(
        ContextId contextId, RankedMapId rankedMapId, int pointId, PointCurveService service, CancellationToken token)
        => Map(await service.GetAsync(contextId, rankedMapId, new Point.PointId(pointId), token));

    private static async Task<IResult> PutAsync(
        ContextId contextId, RankedMapId rankedMapId, int pointId, PointCurveRequests.PutCurve request,
        PointCurveService service, CancellationToken token)
        => Map(await service.PutAsync(contextId, rankedMapId, new Point.PointId(pointId), request, token));

    private static async Task<IResult> DeleteAsync(
        ContextId contextId, RankedMapId rankedMapId, int pointId, PointCurveService service, CancellationToken token)
        => Map(await service.DeleteAsync(contextId, rankedMapId, new Point.PointId(pointId), token));

    private static async Task<IResult> PreviewAsync(
        ContextId contextId, RankedMapId rankedMapId, int pointId, PointCurveRequests.Preview request,
        PointCurveService service, CancellationToken token)
        => Map(await service.PreviewAsync(contextId, rankedMapId, new Point.PointId(pointId), request, token));

    private static IResult Map<T>(Result<T, PointCurveError> result) => result.IsSuccess
        ? TypedResults.Ok(result.Value)
        : result.Error switch
        {
            PointCurveError.NotFound => TypedResults.NotFound(),
            PointCurveError.ReadOnly(var message) => TypedResults.Problem(message, statusCode: 409,
                extensions: new Dictionary<string, object?> { ["code"] = "curve_read_only" }),
            PointCurveError.Validation(var field, var message) => TypedResults.ValidationProblem(
                new Dictionary<string, string[]> { [field] = [message] })
        };
}
