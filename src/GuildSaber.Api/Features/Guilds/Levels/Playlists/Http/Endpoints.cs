using System.Diagnostics;
using System.Net;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Transformers;
using GuildSaber.Common.Settings;
using GuildSaber.Database.Contexts.Server;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ServerRankedMapListLevel = GuildSaber.Database.Models.Server.Guilds.Levels.RankedMapListLevel;
using static GuildSaber.Api.Features.Guilds.Levels.Playlists.Http.PlaylistRequests;
using static GuildSaber.Api.Features.Guilds.Levels.Playlists.Http.PlaylistResponses;

namespace GuildSaber.Api.Features.Guilds.Levels.Playlists.Http;

public class PlaylistEndpoints : IEndpoints
{
    public const string GetLevelPlaylistName = "GetLevelPlaylist";

    public static void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints
        .MapGroup("levels/{levelId}/playlist").WithTag("Levels.Playlists",
            description: "Endpoints for accessing playlists associated with RankingLevelList levels.")
        .MapGet("/", GetLevelPlaylistAsync)
        .WithName(GetLevelPlaylistName)
        .WithSummary("Get the playlist for a ranked map list level.")
        .WithDescription("Get the playlist associated with a ranked map list level by its Id.");

    public static async Task<Results<Ok<Playlist>, NotFound, ProblemHttpResult>> GetLevelPlaylistAsync(
        int levelId, PlaylistFilter filter, PlayerId? playerId,
        IOptions<LinkSettings> linkSettings, ServerDbContext dbContext, LinkGenerator linkGenerator,
        IHttpClientFactory httpClientFactory)
        => await dbContext.Levels
            .OfType<ServerRankedMapListLevel>()
            .Where(x => x.Id == levelId)
            .SelectPlaylists(filter, playerId,
                syncURL: new Uri(linkSettings.Value.ApiBaseUri,
                    linkGenerator.GetPathByName(GetLevelPlaylistName, new { levelId, filter, playerId })).ToString(),
                image: await GetImageDataFromCdnAsync(linkSettings.Value, httpClientFactory, levelId))
            .Match(async query => await query
                        .Cast<Playlist?>()
                        .FirstOrDefaultAsync()
                    switch
                    {
                        null => TypedResults.NotFound(),
                        var playlist => TypedResults.Ok(playlist.Value)
                    },
                error => Task.FromResult((Results<Ok<Playlist>, NotFound, ProblemHttpResult>)
                    TypedResults.Problem(error, statusCode: StatusCodes.Status400BadRequest))
            );

    private static async Task<string?> GetImageDataFromCdnAsync(
        LinkSettings linkSettings, IHttpClientFactory httpClientFactory, int levelId)
    {
        var cdnUri = new Uri(linkSettings.CdnBaseUri, $"levels/{levelId}/cover.png");
        var httpClient = httpClientFactory.CreateClient();
        try
        {
            var imageBytes = await httpClient.GetByteArrayAsync(cdnUri);
            var base64Image = Convert.ToBase64String(imageBytes);
            return $"data:image/png;base64,{base64Image}";
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}

public static class PlaylistExtensions
{
    public static Result<IQueryable<Playlist>> SelectPlaylists(
        this IQueryable<ServerRankedMapListLevel> query,
        PlaylistFilter filter, PlayerId? playerId,
        string? syncURL, string? image) => filter switch
    {
        PlaylistFilter.None => Success(query.Select(PlaylistMappers.MapPlaylistExpression(syncURL, image))),
        PlaylistFilter.NoneWithPassedScores when playerId is null =>
            Failure<IQueryable<Playlist>>("PlayerId is required for PassedScores filter."),
        PlaylistFilter.NoneWithPassedScores => Success(query
            .Select(PlaylistMappers.MapPlaylistPassedScoreExpression(playerId.Value, syncURL, image))),
        PlaylistFilter.NoneWithPassedNorPendingScores when playerId is null =>
            Failure<IQueryable<Playlist>>("PlayerId is required for PassedOrPendingScores filter."),
        PlaylistFilter.NoneWithPassedNorPendingScores => Success(query
            .Select(PlaylistMappers.MapPlaylistPassedOrPendingScoreExpression(playerId.Value, syncURL, image))),
        _ => throw new UnreachableException("Unhandled PlaylistFilter case.")
    };
}