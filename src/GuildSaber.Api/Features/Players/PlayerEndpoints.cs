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

        group.MapDelete("/{playerId}", DeletePlayerAsync)
            .WithName("DeletePlayer")
            .WithSummary("Delete a player")
            .WithDescription("Delete a specific player by their Id.")
            .RequireManager();

        group.MapGet("/lookup/discord/{discordId}", LookupPlayerIdByDiscordIdAsync)
            .WithName("LookupPlayerByDiscordId")
            .WithSummary("Lookup player ID by Discord ID")
            .WithDescription("Resolve a player's ID from their linked Discord account ID.");

        group.MapGet("/lookup/beatleader/{beatleaderId}", LookupPlayerIdByBeatLeaderIdAsync)
            .WithName("LookupPlayerByBeatLeaderId")
            .WithSummary("Lookup player ID by BeatLeader ID")
            .WithDescription("Resolve a player's ID from their linked BeatLeader account ID.");
    }

    private static async Task<Results<Ok<Player>, NotFound>> GetPlayerAsync(
        PlayerId playerId, ServerDbContext dbContext)
        => await dbContext.Players.Where(x => x.Id == playerId)
                .Select(PlayerMappers.MapPlayerExpression)
                .FirstOrDefaultAsync() switch
            {
                null => TypedResults.NotFound(),
                { } player => TypedResults.Ok(player)
            };

    private static async Task<Results<Ok<Player>, NotFound>> GetPlayerAtMeAsync(
        ClaimsPrincipal principal, ServerDbContext dbContext)
        => await GetPlayerAsync(principal.GetPlayerId()!.Value, dbContext);

    private static async Task<Results<Ok<PlayerExtended>, NotFound>> GetPlayerExtendedAsync(
        PlayerId playerId, ServerDbContext dbContext)
        => await dbContext.Players
                .Where(x => x.Id == playerId)
                .Select(PlayerMappers.MapPlayerExtendedExpression)
                .FirstOrDefaultAsync() switch
            {
                null => TypedResults.NotFound(),
                { } playerExtended => TypedResults.Ok(playerExtended)
            };

    private static async Task<Results<Ok<PlayerExtended>, NotFound>> GetPlayerExtendedAtMeAsync(
        ClaimsPrincipal claimsPrincipal, ServerDbContext dbContext)
        => await GetPlayerExtendedAsync(claimsPrincipal.GetPlayerId()!.Value, dbContext);

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
            query = query.Where(x => EF.Functions.ILike(x.Info.Username, $"%{search}%"));

        return TypedResults.Ok(await query
            .ApplySortOrder(sortBy, order)
            .Select(PlayerMappers.MapPlayerExpression)
            .ToPagedListAsync(page, pageSize));
    }

    private static async Task<Results<NoContent, NotFound>> DeletePlayerAsync(
        PlayerId playerId, ServerDbContext dbContext)
    {
        var affectedRows = await dbContext.Players
            .Where(x => x.Id == playerId)
            .ExecuteDeleteAsync();

        return affectedRows > 0
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }

    private static async Task<Results<Ok<PlayerId>, NotFound>> LookupPlayerIdByDiscordIdAsync(
        DiscordId discordId, ServerDbContext dbContext)
        => await dbContext.Players
                .Where(x => x.LinkedAccounts.DiscordId == discordId)
                .Select(x => (PlayerId?)x.Id)
                .FirstOrDefaultAsync() switch
            {
                null => TypedResults.NotFound(),
                { } playerId => TypedResults.Ok(playerId)
            };

    private static async Task<Results<Ok<PlayerId>, NotFound>> LookupPlayerIdByBeatLeaderIdAsync(
        BeatLeaderId beatleaderId, ServerDbContext dbContext)
        => await dbContext.Players
                .Where(x => x.LinkedAccounts.BeatLeaderId == beatleaderId)
                .Select(x => (PlayerId?)x.Id)
                .FirstOrDefaultAsync() switch
            {
                null => TypedResults.NotFound(),
                { } playerId => TypedResults.Ok(playerId)
            };
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