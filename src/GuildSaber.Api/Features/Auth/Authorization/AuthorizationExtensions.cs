using GuildSaber.Database.Models.Server.Guilds.Members;
using Microsoft.AspNetCore.Authorization;

namespace GuildSaber.Api.Features.Auth.Authorization;

public static class AuthorizationExtensions
{
    private const string GuildPermissionPolicyPrefix = "GuildPermission_";

    public static RouteHandlerBuilder RequireManager(this RouteHandlerBuilder builder) => builder
        .RequireAuthorization(AuthConstants.ManagerPolicy);

    public static RouteHandlerBuilder RequireGuildPermission(
        this RouteHandlerBuilder builder, Member.EPermission requiredPermission) => builder
        .RequireAuthorization($"{GuildPermissionPolicyPrefix}{requiredPermission}");

    public static AuthorizationBuilder AddManagerAuthorizationPolicy(this AuthorizationBuilder builder)
        => builder.AddPolicy(AuthConstants.ManagerPolicy, policy => policy.RequireRole(AuthConstants.ManagerRole));

    public static AuthorizationBuilder AddGuildAuthorizationPolicies(this AuthorizationBuilder builder) => builder
        .AddPolicy($"{GuildPermissionPolicyPrefix}{Member.EPermission.GuildLeader}",
            policy => policy.Requirements.Add(new GuildPermissionRequirement(Member.EPermission.GuildLeader)))
        .AddPolicy($"{GuildPermissionPolicyPrefix}{Member.EPermission.RankingTeam}",
            policy => policy.Requirements.Add(new GuildPermissionRequirement(Member.EPermission.RankingTeam)))
        .AddPolicy($"{GuildPermissionPolicyPrefix}{Member.EPermission.ScoringTeam}",
            policy => policy.Requirements.Add(new GuildPermissionRequirement(Member.EPermission.ScoringTeam)))
        .AddPolicy($"{GuildPermissionPolicyPrefix}{Member.EPermission.MemberTeam}",
            policy => policy.Requirements.Add(new GuildPermissionRequirement(Member.EPermission.MemberTeam)));
}