using GuildSaber.Api.Extensions;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds.Levels;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GuildSaber.Api.Features.Guilds.Levels.Playlists;

public class PlaylistEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints
        .MapGet("levels/{levelId}/playlist", GetLevelPlaylistAsync)
        .WithName("GetLevelPlaylist")
        .WithSummary("Get the playlist for a ranked map list level.")
        .WithDescription("Get the playlist associated with a ranked map list level by its Id.");

    public static Results<Ok<PlaylistResponses.Playlist>, NotFound> GetLevelPlaylistAsync(
        int levelId, ServerDbContext dbContext) => dbContext.Levels.OfType<RankedMapListLevel>()
            .Where(x => x.Id == levelId)
            .Select(PlaylistMappers.MapPlaylistExpression(null, null))
            .Cast<PlaylistResponses.Playlist?>()
            .FirstOrDefault() switch
        {
            null => TypedResults.NotFound(),
            var playlist => TypedResults.Ok(playlist.Value)
        };
}