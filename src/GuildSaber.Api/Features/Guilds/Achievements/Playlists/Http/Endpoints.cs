using System.Diagnostics;
using System.Net;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Transformers;
using GuildSaber.Common.Settings;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds.Achievements;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static GuildSaber.Api.Features.Guilds.Achievements.Playlists.Http.PlaylistRequests;
using static GuildSaber.Api.Features.Guilds.Achievements.Playlists.Http.PlaylistResponses;

namespace GuildSaber.Api.Features.Guilds.Achievements.Playlists.Http;

public class PlaylistEndpoints : IEndpoints
{
    public const string GetAchievementPlaylistName = "GetAchievementPlaylist";

    public static void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints
        .MapGroup("achievements/{achievementId}/playlist").WithTag("Achievements.Playlists",
            description: "Endpoints for accessing playlists associated with Achievements.")
        .MapGet("/", GetAchievementPlaylistAsync)
        .WithName(GetAchievementPlaylistName)
        .WithSummary("Get the playlist for an achievement.")
        .WithDescription("Get the playlist associated with an achievement by its Id.");

    public static async Task<Results<Ok<Playlist>, NotFound, ProblemHttpResult>> GetAchievementPlaylistAsync(
        int achievementId, PlaylistFilter filter, PlayerId? playerId,
        IOptions<LinkSettings> linkSettings, ServerDbContext dbContext, LinkGenerator linkGenerator,
        IHttpClientFactory httpClientFactory)
        => await dbContext.Achievements
            .Where(x => x.Id == achievementId)
            .SelectPlaylists(dbContext, filter, playerId,
                syncURL: new Uri(linkSettings.Value.ApiBaseUri,
                    linkGenerator.GetPathByName(GetAchievementPlaylistName,
                        new { achievementId, filter, playerId })).ToString(),
                image: await GetImageDataFromCdnAsync(linkSettings.Value, httpClientFactory, achievementId))
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
        LinkSettings linkSettings, IHttpClientFactory httpClientFactory, int achievementId)
    {
        var cdnUri = new Uri(linkSettings.CdnBaseUri, $"achievements/{achievementId}/cover.png");
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
        this IQueryable<Achievement> query,
        ServerDbContext dbContext,
        PlaylistFilter filter, PlayerId? playerId,
        string? syncURL, string? image) => filter switch
    {
        PlaylistFilter.None => Success(query.Select(PlaylistMappers.MapPlaylistExpression(
            dbContext, syncURL, image))),
        PlaylistFilter.NoneWithPassedScores when playerId is null =>
            Failure<IQueryable<Playlist>>("PlayerId is required for PassedScores filter."),
        PlaylistFilter.NoneWithPassedScores => Success(query
            .Select(PlaylistMappers.MapPlaylistPassedScoreExpression(
                dbContext, playerId.Value, syncURL, image))),
        PlaylistFilter.NoneWithPassedNorPendingScores when playerId is null =>
            Failure<IQueryable<Playlist>>("PlayerId is required for PassedOrPendingScores filter."),
        PlaylistFilter.NoneWithPassedNorPendingScores => Success(query
            .Select(PlaylistMappers.MapPlaylistPassedOrPendingScoreExpression(
                dbContext, playerId.Value, syncURL, image))),
        _ => throw new UnreachableException("Unhandled PlaylistFilter case.")
    };
}