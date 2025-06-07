using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.Auth;
using GuildSaber.Api.Features.Internal;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Players;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.Players;

public class PlayerEndpoints : IEndPoints
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/players")
            .WithTag("Players", description: "Endpoints for managing players");

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
            .WithSummary("Get current player enriched")
            .WithDescription("Get the current player information alongside their guilds and permissions.")
            .RequireAuthorization();
    }

    private static async Task<Results<Ok<PlayerResponses.PlayerAtMe>, NotFound>> GetPlayerAtMeAsync(
        ServerDbContext dbContext, ClaimsPrincipal claimsPrincipal)
        => await dbContext.Players
                .Where(x => x.Id == new Player.PlayerId(
                    uint.Parse(((ClaimsIdentity)claimsPrincipal.Identity!).FindFirst(AuthConstants.PlayerIdClaimType)!
                        .Value)))
                .Select(PlayerMappers.MapPlayerAtMeExpression)
                .FirstOrDefaultAsync() switch
            {
                { Player.Id: 0 } => TypedResults.NotFound(),
                var player => TypedResults.Ok(player)
            };

    private static async Task<Results<Ok<PlayerResponses.Player>, NotFound>> GetPlayerAsync(
        Player.PlayerId playerId, ServerDbContext dbContext)
        => await dbContext.Players.Where(x => x.Id == playerId)
                .Select(PlayerMappers.MapPlayerExpression)
                .FirstOrDefaultAsync() switch
            {
                { Id: 0 } => TypedResults.NotFound(),
                var player => TypedResults.Ok(player)
            };

    private static async Task<Results<Ok<PagedList<PlayerResponses.Player>>, ProblemHttpResult>> GetPlayersAsync(
        ServerDbContext dbContext,
        [Range(1, int.MaxValue)] int page = 1,
        [Range(1, 100)] int pageSize = 10,
        string? search = null,
        PlayerRequests.EPlayerSorters sortBy = PlayerRequests.EPlayerSorters.CreationDate,
        EOrder order = EOrder.Desc)
    {
        var query = dbContext.Players.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Info.Username.Contains(search) || x.Info.Country.Contains(search));

        query = ApplySortOrder(query, sortBy, order);

        return TypedResults.Ok(await PagedList<PlayerResponses.Player>
            .CreateAsync(query.Select(PlayerMappers.MapPlayerExpression), page, pageSize));
    }

    private static IQueryable<Player> ApplySortOrder(
        IQueryable<Player> query, PlayerRequests.EPlayerSorters sortBy, EOrder order) => sortBy switch
    {
        PlayerRequests.EPlayerSorters.Id => query.OrderBy(order, x => x.Id),
        PlayerRequests.EPlayerSorters.CreationDate => query.OrderBy(order, x => x.Info.CreatedAt)
            .ThenBy(order, x => x.Id),
        _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null)
    };
}