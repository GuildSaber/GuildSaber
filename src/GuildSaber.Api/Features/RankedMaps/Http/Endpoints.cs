using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Security.Claims;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.Auth.Authorization;
using GuildSaber.Api.Features.RankedScores.Http;
using GuildSaber.Api.Shared;
using GuildSaber.Api.Transformers;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.RankedScores;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ServerRankedMap = GuildSaber.Database.Models.Server.RankedMaps.RankedMap;
using static GuildSaber.Api.Features.RankedMaps.Http.RankedMapRequests;
using static GuildSaber.Api.Features.RankedMaps.Http.RankedMapResponses;

namespace GuildSaber.Api.Features.RankedMaps.Http;

public class RankedMapEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var rankedMapGroup = endpoints.MapGroup("ranked-maps")
            .WithTag("RankedMaps", description: "Endpoints for accessing ranked maps in the server.");

        rankedMapGroup.MapGet("/{rankedMapId}", GetRankedMapAsync)
            .WithName("GetRankedMap")
            .WithSummary("Get a ranked map.")
            .WithDescription("Get a ranked map in the server by its Id.");

        rankedMapGroup.MapGet("/{rankedMapId}/with-scores/{playerId}", GetRankedMapWithScoresAsync)
            .WithName("GetRankedMapWithScores")
            .WithSummary("Get a ranked map with the player's point scores.")
            .WithDescription(
                "Get a ranked map in the server by its Id, including the player's best point scores on the map.");

        rankedMapGroup.MapGet("/{rankedMapId}/with-scores/@me", GetRankedMapWithScoresAtMeAsync)
            .WithName("GetRankedMapWithScoresAtMe")
            .WithSummary("Get a ranked map with the current player's point scores.")
            .WithDescription(
                "Get a ranked map in the server by its Id, including the current player's best point scores on the map.")
            .RequireAuthorization();

        var group = endpoints.MapGroup("contexts/{contextId}/ranked-maps")
            .WithTag("Context.RankedMaps", description: "Endpoints for managing ranked maps within a context.");

        group.MapGet("/", GetRankedMapsAsync)
            .WithName("GetRankedMaps")
            .WithSummary("Get ranked maps for a context.")
            .WithDescription("Get ranked maps for a context by its Id, with optional search and sorting.");

        group.MapGet("/with-scores/{playerId}", GetRankedMapsWithScoresAsync)
            .WithName("GetRankedMapsWithScores")
            .WithSummary("Get ranked maps for a context with the player's point scores.")
            .WithDescription("Get ranked maps for a context by its Id, with optional search and sorting, including " +
                             "the player's best point scores on each map.");

        group.MapGet("/with-scores/@me", GetRankedMapsWithScoresAtMeAsync)
            .WithName("GetRankedMapsWithScoresAtMe")
            .WithSummary("Get ranked maps for a context with the current player's point scores.")
            .WithDescription("Get ranked maps for a context by its Id, with optional search and sorting, including " +
                             "the current player's best point scores on each map.")
            .RequireAuthorization();
    }

    private static async Task<Results<Ok<RankedMapWithScores>, NotFound>> GetRankedMapWithScoresAtMeAsync(
        RankedMapId rankedMapId,
        ServerDbContext dbContext,
        ClaimsPrincipal claimsPrincipal)
        => await GetRankedMapWithScoresAsync(rankedMapId, claimsPrincipal.GetPlayerId()!.Value, dbContext);

    private static async Task<Results<Ok<RankedMapWithScores>, NotFound>> GetRankedMapWithScoresAsync(
        RankedMapId rankedMapId,
        PlayerId playerId,
        ServerDbContext dbContext) => await dbContext.RankedMaps.AsExpandable()
            .Where(x => x.Id == rankedMapId)
            .Select(RankedMapMappers.MapRankedMapWithScoresExpression(playerId))
            .FirstOrDefaultAsync() switch
        {
            null => TypedResults.NotFound(),
            var rankedMap => TypedResults.Ok(rankedMap)
        };

    private static async Task<Results<Ok<RankedMap>, NotFound>> GetRankedMapAsync(
        RankedMapId rankedMapId, ServerDbContext dbContext) => await dbContext.RankedMaps.AsExpandable()
            .Where(x => x.Id == rankedMapId)
            .Select(RankedMapMappers.MapRankedMapExpression())
            .FirstOrDefaultAsync() switch
        {
            null => TypedResults.NotFound(),
            var rankedMap => TypedResults.Ok(rankedMap)
        };

    private static async Task<Ok<PagedList<RankedMapSimple>>> GetRankedMapsAsync(
        [FromRoute] ContextId contextId,
        ServerDbContext dbContext,
        [AsParameters] Filters filters,
        [Range(1, int.MaxValue)] int page = 1,
        [Range(1, 100)] int pageSize = 10,
        ERankedMapSorter sortBy = ERankedMapSorter.DifficultyStar,
        EOrder order = EOrder.Asc)
        => TypedResults.Ok(await dbContext.RankedMaps.AsSplitQuery().Where(x => x.ContextId == contextId)
            .ApplyFilters(filters, null)
            .ApplySortOrder(sortBy, order, null)
            .Select(RankedMapMappers.MapRankedMapSimpleExpression())
            .ToPagedListAsync(page, pageSize));

    private static async Task<Ok<PagedList<RankedMapWithScores>>> GetRankedMapsWithScoresAtMeAsync(
        [FromRoute] ContextId contextId,
        ServerDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        [AsParameters] Filters filters,
        [Range(1, int.MaxValue)] int page = 1,
        [Range(1, 100)] int pageSize = 10,
        ERankedMapSorter sortBy = ERankedMapSorter.DifficultyStar,
        EOrder order = EOrder.Asc)
        => await GetRankedMapsWithScoresAsync(contextId, claimsPrincipal.GetPlayerId()!.Value, dbContext, filters,
            page, pageSize, sortBy, order);

    private static async Task<Ok<PagedList<RankedMapWithScores>>> GetRankedMapsWithScoresAsync(
        [FromRoute] ContextId contextId,
        [FromRoute] PlayerId playerId,
        ServerDbContext dbContext,
        [AsParameters] Filters filters,
        [Range(1, int.MaxValue)] int page = 1,
        [Range(1, 100)] int pageSize = 10,
        ERankedMapSorter sortBy = ERankedMapSorter.DifficultyStar,
        EOrder order = EOrder.Asc)
        => TypedResults.Ok(await dbContext.RankedMaps.AsExpandable()
            .Where(x => x.ContextId == contextId)
            .ApplyFilters(filters, playerId)
            .ApplySortOrder(sortBy, order, playerId)
            .Select(RankedMapMappers.MapRankedMapWithScoresExpression(playerId))
            .ToPagedListAsync(page, pageSize));
}

public static class RankedMapExtensions
{
    extension(IQueryable<ServerRankedMap> query)
    {
        public IQueryable<ServerRankedMap> ApplyFilters(Filters filters, PlayerId? playerId)
        {
            if (filters.CategoryIds is { Length: > 0 } categoryIds)
                query = filters.MatchAnyCategory
                    ? query.Where(x => x.Categories.Any(c => ((IEnumerable<int>)categoryIds).Contains(c.Id)))
                    : query.Where(x => categoryIds.All(id => x.Categories.Any(c => c.Id == id)));

            // Filtering only applies when there is a player and a filter for their scores, otherwise the map is gonna be included.
            if (playerId is not null && filters.RankedScoreTypes is not RankedScoreRequests.ERankedScoreType.None)
            {
                var validFilter = filters.RankedScoreTypes.HasFlag(RankedScoreRequests.ERankedScoreType.Valid);
                var invalidFilter = filters.RankedScoreTypes.HasFlag(RankedScoreRequests.ERankedScoreType.Invalid);
                var pendingFilter = filters.RankedScoreTypes.HasFlag(RankedScoreRequests.ERankedScoreType.Pending);
                var acceptedFilter = filters.RankedScoreTypes.HasFlag(RankedScoreRequests.ERankedScoreType.Accepted);
                var refusedFilter = filters.RankedScoreTypes.HasFlag(RankedScoreRequests.ERankedScoreType.Refused);

                var playerHaveNoScore = (Expression<Func<ServerRankedMap, bool>>)
                    (map => !map.RankedScores.Any(rs => rs.PlayerId == playerId.Value && rs.IsSelected));

                var scoreTypeFilter = (Expression<Func<ServerRankedMap, bool>>)
                    (map => map.RankedScores.Any(rs => rs.PlayerId == playerId.Value && rs.IsSelected && (
                        validFilter && rs is ValidRankedScore
                        || invalidFilter && rs is InvalidRankedScore
                        || pendingFilter && rs is PendingRankedScore
                        || acceptedFilter && rs is AcceptedRankedScore
                        || refusedFilter && rs is RefusedRankedScore
                    )));

                query = filters.IncludeMapsWithoutScore
                    // Allow when there is no score, but apply the score type filter when the player have a score.
                    ? query.Where(playerHaveNoScore.Or(scoreTypeFilter))
                    // Only include maps where the player have a score of the specified types.
                    : query.Where(scoreTypeFilter);
            }

            if (filters.DifficultyStarFrom is { } difficultyStarFrom)
                query = query.Where(x => x.Rating.DiffStar >= difficultyStarFrom);

            if (filters.DifficultyStarTo is { } difficultyStarTo)
                query = query.Where(x => x.Rating.DiffStar <= difficultyStarTo);

            if (filters.AccuracyStarFrom is { } accuracyStarFrom)
                query = query.Where(x => x.Rating.AccStar >= accuracyStarFrom);

            if (filters.AccuracyStarTo is { } accuracyStarTo)
                query = query.Where(x => x.Rating.AccStar <= accuracyStarTo);

            if (filters.DurationSecFrom is { } durationSecFrom)
                query = query.Where(x => x.MapVersions.Any(v => v.Song.Stats.DurationSec >= durationSecFrom));

            if (filters.DurationSecTo is { } durationSecTo)
                query = query.Where(x => x.MapVersions.Any(v => v.Song.Stats.DurationSec <= durationSecTo));

            if (filters.BpmFrom is { } bpmFrom)
                query = query.Where(x => x.MapVersions.Any(v => v.Song.Stats.BPM >= bpmFrom));

            if (filters.BpmTo is { } bpmTo)
                query = query.Where(x => x.MapVersions.Any(v => v.Song.Stats.BPM <= bpmTo));

            if (filters.NeedConfirmation is { } needConfirmation)
                query = query.Where(x => x.Requirements.NeedConfirmation == needConfirmation);

            return query.ApplyMapSearch(filters.Search);
        }

        [SuppressMessage("ReSharper", "InvertIf")]
        private IQueryable<ServerRankedMap> ApplyMapSearch(string? search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return query;

            switch (search.Length)
            {
                case SongHash.ExactLength:
                    search = search.ToLowerInvariant();
                    return query.Where(x => x.MapVersions.Any(version => version.Song.Hash == search));
                case >= BeatSaverKey.BsrPrefixLength and <= BeatSaverKey.BsrPrefixLength + BeatSaverKey.MaxLength
                    when search.StartsWith("!bsr "):
                    search = search[5..];
                    return query.Where(x => x.MapVersions.Any(version => version.Song.BeatSaverKey == search));
                default:
                    return query.Where(x => x.MapVersions.Any(version =>
                        EF.Functions.ILike(version.Song.Info.SongName, $"%{search}%") ||
                        EF.Functions.ILike(version.Song.Info.SongAuthorName, $"%{search}%") ||
                        EF.Functions.ILike(version.Song.Info.MapperName, $"%{search}%") ||
                        search.Length <= BeatSaverKey.MaxLength
                        && version.Song.BeatSaverKey != null
                        && EF.Functions.ILike(version.Song.BeatSaverKey, $"%{search}%")));
            }
        }

        public IQueryable<ServerRankedMap> ApplySortOrder(ERankedMapSorter sortBy, EOrder order, PlayerId? playerId)
            => sortBy switch
            {
                ERankedMapSorter.Id => query.OrderBy(order, x => x.Id),
                ERankedMapSorter.EditTime => query.OrderBy(order, x => x.Info.EditedAt)
                    .ThenBy(order, guild => guild.Id),
                ERankedMapSorter.RankTime => query.OrderBy(order, x => x.Info.CreatedAt)
                    .ThenBy(order, x => x.Id),
                ERankedMapSorter.MapUploadTime => query.OrderBy(order, x => x.MapVersions
                        .Select(v => v.Song.UploadedAt)
                        .FirstOrDefault())
                    .ThenBy(order, x => x.Id),
                ERankedMapSorter.RankedScoreTime => query.OrderBy(order, x => x.RankedScores
                        .Where(rs => rs.PlayerId == playerId && rs.IsSelected)
                        .Select(rs => rs.EditedAt)
                        .FirstOrDefault())
                    .ThenBy(order, x => x.Id),
                ERankedMapSorter.DifficultyStar => query.OrderBy(order, x => x.Rating.DiffStar)
                    .ThenBy(order, x => x.Id),
                ERankedMapSorter.AccuracyStar => query.OrderBy(order, x => x.Rating.AccStar)
                    .ThenBy(order, x => x.Id),
                ERankedMapSorter.Name => query.OrderBy(order, x => x.MapVersions
                        .Select(v => v.Song.Info.SongName)
                        .FirstOrDefault())
                    .ThenBy(order, x => x.Id),
                _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null)
            };
    }

    private static Expression<Func<ServerRankedMap, bool>> MapsHaveNoPlayerScore(PlayerId playerId)
        => map => !map.RankedScores.Any(rs => rs.PlayerId == playerId && rs.IsSelected);
}