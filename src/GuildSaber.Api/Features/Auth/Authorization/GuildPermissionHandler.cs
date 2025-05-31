using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Members;
using Microsoft.AspNetCore.Authorization;

namespace GuildSaber.Api.Features.Auth.Authorization;

public class GuildPermissionHandler : AuthorizationHandler<GuildPermissionRequirement, Guild.GuildId>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, GuildPermissionRequirement requirement, Guild.GuildId guildId)
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
                _ => user.FindFirst(AuthConstants.GuildPermissionClaimType(guildId.ToString())) switch
                {
                    { Value: { } value }
                        when Enum.TryParse<Member.EPermission>(value.AsSpan(), out var permission)
                             && permission.HasFlag(requirement.RequiredPermission)
                        => Succeed(context, requirement),
                    _ => Fail(context)
                }
            }
        };

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