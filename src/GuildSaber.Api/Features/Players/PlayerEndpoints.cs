using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.Auth.Authorization;
using GuildSaber.Api.Features.Internal;
using GuildSaber.Api.Transformers;
using GuildSaber.Database.Contexts.Server;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using static GuildSaber.Api.Features.Players.PlayerResponses;
using ServerPlayer = GuildSaber.Database.Models.Server.Players.Player;

namespace GuildSaber.Api.Features.Players;

public class PlayerEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/players")
            .WithTag("Players", description: "Endpoints for managing players' ranked scores.");

        group.MapGet("/", GetPlayersAsync)
            .WithName("GetPlayers")
            .WithSummary("Get all players paginated")
            .WithDescription("Get all players in the server, with optional search and sorting.");

        group.MapGet("/{playerId}", GetPlayerAsync)
            .WithName("GetPlayer")
            .WithSummary("Get a player")
            .WithDescription("Get a specific player by their Id.");

        group.MapGet("/@me", GetPlayerAtMeAsync)
            .WithName("GetPlayerAtMe")
            .WithSummary("Get current player")
            .WithDescription("Get the current player information.")
            .RequireAuthorization();

        group.MapGet("/{playerId}/extended", GetPlayerExtendedAsync)
            .WithName("GetPlayerExtended")
            .WithSummary("Get a player with extended information")
            .WithDescription("Get a specific player with extended information by their Id."
                             + " Which includes additional fields like guilds and permissions.");

        group.MapGet("/@me/extended", GetPlayerExtendedAtMeAsync)
            .WithName("GetPlayerExtendedAtMe")
            .WithSummary("Get current player with extended information")
            .WithDescription("Get the current player information alongside their guilds and permissions.")
            .RequireAuthorization();
    }

    private static async Task<Results<Ok<Player>, NotFound>> GetPlayerAsync(
        PlayerId playerId, ServerDbContext dbContext)
        => await dbContext.Players.Where(x => x.Id == playerId)
                .Select(PlayerMappers.MapPlayerExpression)
                .Cast<Player?>()
                .FirstOrDefaultAsync() switch
            {
                null => TypedResults.NotFound(),
                { } player => TypedResults.Ok(player)
            };

    private static Task<Results<Ok<Player>, NotFound>> GetPlayerAtMeAsync(
        ClaimsPrincipal principal, ServerDbContext dbContext)
        => GetPlayerAsync(principal.GetPlayerId()!.Value, dbContext);

    private static async Task<Results<Ok<PlayerExtended>, NotFound>> GetPlayerExtendedAsync(
        PlayerId playerId, ServerDbContext dbContext)
        => await dbContext.Players
                .Where(x => x.Id == playerId)
                .Select(PlayerMappers.MapPlayerExtendedExpression)
                .Cast<PlayerExtended?>()
                .FirstOrDefaultAsync() switch
            {
                null => TypedResults.NotFound(),
                { } playerExtended => TypedResults.Ok(playerExtended)
            };

    private static Task<Results<Ok<PlayerExtended>, NotFound>> GetPlayerExtendedAtMeAsync(
        ClaimsPrincipal claimsPrincipal, ServerDbContext dbContext)
        => GetPlayerExtendedAsync(claimsPrincipal.GetPlayerId()!.Value, dbContext);

    private static async Task<Ok<PagedList<Player>>> GetPlayersAsync(
        ServerDbContext dbContext,
        [Range(1, int.MaxValue)] int page = 1,
        [Range(1, 100)] int pageSize = 10,
        string? search = null,
        PlayerRequests.EPlayerSorter sortBy = PlayerRequests.EPlayerSorter.CreationDate,
        EOrder order = EOrder.Desc)
    {
        var query = dbContext.Players.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Info.Username.Contains(search) || x.Info.Country.Contains(search));

        return TypedResults.Ok(await query
            .ApplySortOrder(sortBy, order)
            .Select(PlayerMappers.MapPlayerExpression)
            .ToPagedListAsync(page, pageSize));
    }
}

public static class PlayerExtensions
{
    public static IQueryable<ServerPlayer> ApplySortOrder(
        this IQueryable<ServerPlayer> query, PlayerRequests.EPlayerSorter sortBy, EOrder order) => sortBy switch
    {
        PlayerRequests.EPlayerSorter.Id => query
            .OrderBy(order, x => x.Id),
        PlayerRequests.EPlayerSorter.CreationDate => query.OrderBy(order, x => x.Info.CreatedAt)
            .ThenBy(order, x => x.Id),
        _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null)
    };
}