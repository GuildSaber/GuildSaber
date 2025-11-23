using System.Security.Claims;
using GuildSaber.Database.Contexts.Server;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace GuildSaber.Api.Features.Auth.Authorization;

public class GuildPermissionHandler(
    IServiceScopeFactory serviceScopeFactory,
    IHttpContextAccessor httpContextAccessor,
    HybridCache cache) : AuthorizationHandler<GuildPermissionRequirement>
{
    private const string GuildIdRouteKey = "guildId";
    private const string ContextIdRouteKey = "contextId";
    protected const string ContextIdToGuildIdCacheKeyPrefix = "ContextIdToGuildId_";

    internal static ValueTask<string?> GetGuildIdFromContextAsync(
        ContextId contextId, IServiceScopeFactory serviceScopeFactory, HybridCache cache)
        => cache.GetOrCreateAsync($"{ContextIdToGuildIdCacheKeyPrefix}{contextId}", (serviceScopeFactory, contextId),
            async static (state, token) =>
            {
                await using var scope = state.serviceScopeFactory.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

                return (await dbContext.Contexts
                        .FirstOrDefaultAsync(x => x.Id == state.contextId, token))
                    ?.GuildId.ToString();
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(10)
            });

    /// <summary>
    /// Handles the authorization requirement for guild permissions.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="requirement"></param>
    /// <remarks>
    /// Returning CompletedTask => Unauthorized
    /// Returning Succeed => Authorized
    /// Returning Fail => Forbidden
    /// No valid guildId and no valid contextId in route => Unauthorized
    /// </remarks>
    /// <returns></returns>
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, GuildPermissionRequirement requirement)
        => await (context switch
        {
            _ when requirement.RequiredPermission == EPermission.None
                => Succeed(context, requirement),
            { User.Identity: null or { IsAuthenticated: false } }
                => Task.CompletedTask,
            { User: var user } => user switch
            {
                _ when user.IsInRole(AuthConstants.ManagerRole)
                    => Succeed(context, requirement),
                _ when FindGuildIdInHttpContextAccessor(httpContextAccessor) is { } guildId
                    => CheckGuildPermissionFromClaims(user, guildId, requirement, context),
                _ when FindContextIdInHttpContextAccessor(httpContextAccessor) is { } contextIdStr
                       && ContextId.TryParse(contextIdStr, out var contextId)
                    => await GetGuildIdFromContextAsync(contextId, serviceScopeFactory, cache) is { } guildId
                        ? CheckGuildPermissionFromClaims(user, guildId, requirement, context)
                        : Fail(context),
                _ => Fail(context)
            }
        });

    /// <summary>
    /// Checks if the user has the required guild permission based on their claims.
    /// </summary>
    /// <param name="user">The claims principal representing the authenticated user.</param>
    /// <param name="guildId">The unique identifier of the guild to check permissions for.</param>
    /// <param name="requirement">The guild permission requirement to validate.</param>
    /// <param name="context">The authorization context.</param>
    /// <returns>
    /// A task that completes when authorization check is done.
    /// Succeeds if user has the required permission claim for the guild, fails otherwise.
    /// </returns>
    /// <remarks>
    /// This method looks for a guild-specific permission claim in the user's claims.
    /// The claim type is determined by <see cref="AuthConstants.GuildPermissionClaimType" />.
    /// The permission value must match or include the required permission flags.
    /// </remarks>
    private static Task CheckGuildPermissionFromClaims(
        ClaimsPrincipal user,
        string guildId,
        GuildPermissionRequirement requirement,
        AuthorizationHandlerContext context) => user.FindFirst(AuthConstants.GuildPermissionClaimType(guildId)) switch
    {
        { Value: var perm } when Enum.TryParse<EPermission>(perm.AsSpan(), out var permission)
                                 && permission.HasFlag(requirement.RequiredPermission)
            => Succeed(context, requirement),
        _ => Fail(context)
    };

    /// <summary>
    /// Try to find the GuildId from the route values in the current HTTP context.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    /// <returns>The GuildId as a string if found; otherwise, null.</returns>
    private static string? FindGuildIdInHttpContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        if (httpContextAccessor.HttpContext is null)
            return null;

        if (httpContextAccessor.HttpContext.Request.RouteValues
                .TryGetValue(GuildIdRouteKey, out var guildIdObj) && guildIdObj is string guildIdStr)
            return guildIdStr;

        return null;
    }

    /// <summary>
    /// Try to find the ContextId from the route values in the current HTTP context.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    /// <returns> The ContextId if found; otherwise, null.</returns>
    private static string? FindContextIdInHttpContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        if (httpContextAccessor.HttpContext is null)
            return null;

        if (httpContextAccessor.HttpContext.Request.RouteValues
                .TryGetValue(ContextIdRouteKey, out var contextIdObj) && contextIdObj is string contextIdStr)
            return contextIdStr;

        return null;
    }

    private static Task Succeed(AuthorizationHandlerContext context, GuildPermissionRequirement requirement)
    {
        context.Succeed(requirement);
        return Task.CompletedTask;
    }

    private static Task Fail(AuthorizationHandlerContext context)
    {
        context.Fail();
        return Task.CompletedTask;
    }
}