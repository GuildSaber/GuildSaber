using GuildSaber.Database.Models.Server.Guilds.Members;
using Microsoft.AspNetCore.Authorization;

namespace GuildSaber.Api.Features.Auth.Authorization;

public class GuildPermissionHandler(IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<GuildPermissionRequirement>
{
    private const string GuildIdRouteKey = "guildId";

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, GuildPermissionRequirement requirement)
        => context switch
        {
            _ when requirement.RequiredPermission == Member.EPermission.None
                => Succeed(context, requirement),
            { User.Identity: null or { IsAuthenticated: false } }
                => Task.CompletedTask, // Should mean unauthorized because it doesn't succeed.
            { User: var user } => user switch
            {
                _ when user.FindFirst(AuthConstants.ManagerClaimType) is not null
                    => Succeed(context, requirement),
                _ when TryGetGuildId(httpContextAccessor, out var guildId) => user.FindFirst(
                        AuthConstants.GuildPermissionClaimType(guildId)) switch
                    {
                        { Value: { } value }
                            when Enum.TryParse<Member.EPermission>(value.AsSpan(), out var permission)
                                 && permission.HasFlag(requirement.RequiredPermission)
                            => Succeed(context, requirement),
                        _ => Fail(context)
                    },
                _ => Fail(context) // No guild ID in route, so we can't check permissions.
            }
        };

    /// <summary>
    /// Get the guild ID from the route values in the current HTTP context.
    /// </summary>
    /// <param name="httpContextAccessor"></param>
    /// <param name="guildId"></param>
    /// <returns></returns>
    private static bool TryGetGuildId(IHttpContextAccessor httpContextAccessor, out string guildId)
    {
        if (httpContextAccessor.HttpContext is null)
        {
            guildId = string.Empty;
            return false;
        }

        if (httpContextAccessor.HttpContext.Request.RouteValues.TryGetValue(GuildIdRouteKey, out var guildIdObj)
            && guildIdObj is string guildIdStr)
        {
            guildId = guildIdStr;
            return true;
        }

        guildId = string.Empty;
        return false;
    }

    private static Task Succeed(AuthorizationHandlerContext context, GuildPermissionRequirement requirement)
    {
        context.Succeed(requirement);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Should return a Forbidden.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    private static Task Fail(AuthorizationHandlerContext context)
    {
        context.Fail();
        return Task.CompletedTask;
    }
}