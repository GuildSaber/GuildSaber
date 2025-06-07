using System.ComponentModel.DataAnnotations;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.Auth.Authorization;
using GuildSaber.Api.Features.Internal;
using GuildSaber.Database.Contexts.Server;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using static GuildSaber.Api.Features.Guilds.GuildRequests;
using Guild = GuildSaber.Database.Models.Server.Guilds.Guild;

namespace GuildSaber.Api.Features.Guilds;

public class GuildEndpoints : IEndPoints
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/guilds")
            .WithTag("Guilds", description: "Endpoints for managing guilds in the server.");

        group.MapGet("/", GetGuildsAsync)
            .WithName("GetGuilds")
            .WithSummary("Get all guilds paginated")
            .WithDescription("Get all guilds in the server, with optional search and sorting.");

        group.MapGet("/{guildId}", GetGuildAsync)
            .WithName("GetGuild")
            .WithSummary("Get a guild")
            .WithDescription("Get a specific guild by its Id.");

        group.MapGet("/{guildId}/extended", GetGuildExtended)
            .WithName("GetGuildExtended")
            .WithSummary("Get Guild with Extended Information")
            .WithDescription("Get a specific guild with extended information by its Id."
                             + " Which includes additional fields like categories and points.");

        group.MapDelete("/{guildId}", DeleteGuildAsync)
            .WithName("DeleteGuild")
            .WithSummary("Delete a guild")
            .WithDescription("Delete a guild by its Id.")
            .RequireManager();
    }

    private static async Task<Results<NoContent, NotFound>> DeleteGuildAsync(
        Guild.GuildId guildId, ServerDbContext dbContext)
    {
        var affectedRows = await dbContext.Guilds
            .Where(x => x.Id == guildId)
            .ExecuteDeleteAsync();

        return affectedRows > 0
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }

    private static async Task<Ok<PagedList<GuildResponses.Guild>>> GetGuildsAsync(
        ServerDbContext dbContext,
        [Range(1, int.MaxValue)] int page = 1,
        [Range(1, 100)] int pageSize = 10,
        string? search = null,
        EGuildSorters sortBy = EGuildSorters.Popularity,
        EOrder order = EOrder.Desc)
    {
        var query = dbContext.Guilds.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => ((string)x.Info.Name).Contains(search));

        query = ApplySortOrder(query, sortBy, order);

        return TypedResults.Ok(await PagedList<GuildResponses.Guild>
            .CreateAsync(query.Select(GuildMappers.MapGuildExpression), page, pageSize)
        );
    }

    private static async Task<Results<Ok<GuildResponses.Guild>, NotFound>> GetGuildAsync(
        Guild.GuildId guildId, ServerDbContext dbContext)
        => await dbContext.Guilds.Where(x => x.Id == guildId)
                .Select(GuildMappers.MapGuildExpression)
                .FirstOrDefaultAsync()
            switch
            {
                { Id: 0 } => TypedResults.NotFound(),
                var guild => TypedResults.Ok(guild)
            };

    private static async Task<Results<Ok<GuildResponses.GuildExtended>, NotFound>> GetGuildExtended(
        Guild.GuildId guildId, ServerDbContext dbContext)
        => await dbContext.Guilds.AsSplitQuery()
                .Where(x => x.Id == guildId)
                .Select(GuildMappers.MapGuildExtendedExpression)
                .FirstOrDefaultAsync() switch
            {
                { Guild.Id: 0 } => TypedResults.NotFound(),
                var guildExtended => TypedResults.Ok(guildExtended)
            };

    private static IQueryable<Guild> ApplySortOrder(IQueryable<Guild> query, EGuildSorters sortBy, EOrder order)
        => sortBy switch
        {
            EGuildSorters.Id => query.OrderBy(order, guild => guild.Id),
            EGuildSorters.Name => query.OrderBy(order, guild => guild.Info.Name),
            EGuildSorters.Popularity => query.OrderBy(order, guild => guild.Status)
                .ThenBy(order, guild => guild.RankedScores.Count / guild.Members.Count),
            EGuildSorters.CreationDate => query.OrderBy(order, guild => guild.Info.CreatedAt),
            EGuildSorters.MemberCount => query.OrderBy(order, guild => guild.Members.Count),
            EGuildSorters.MapCount => query.OrderBy(order, guild => guild.RankedMaps.Count),
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null)
        };
}