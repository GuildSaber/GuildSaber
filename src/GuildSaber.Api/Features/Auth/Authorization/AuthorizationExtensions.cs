using Microsoft.AspNetCore.Authorization;

namespace GuildSaber.Api.Features.Auth.Authorization;

public static class AuthorizationExtensions
{
    private const string GuildPermissionPolicyPrefix = "GuildPermission_";

    public static RouteHandlerBuilder RequireManager(this RouteHandlerBuilder builder) => builder
        .RequireAuthorization(AuthConstants.ManagerPolicy);

    public static RouteHandlerBuilder RequireGuildPermission(
        this RouteHandlerBuilder builder, EPermission requiredPermission) => builder
        .RequireAuthorization($"{GuildPermissionPolicyPrefix}{requiredPermission}");

    public static AuthorizationBuilder AddManagerAuthorizationPolicy(this AuthorizationBuilder builder)
        => builder.AddPolicy(AuthConstants.ManagerPolicy, policy => policy.RequireRole(AuthConstants.ManagerRole));

    public static AuthorizationBuilder AddGuildAuthorizationPolicies(this AuthorizationBuilder builder) => builder
        .AddPolicy($"{GuildPermissionPolicyPrefix}{EPermission.GuildLeader}",
            policy => policy.Requirements.Add(new GuildPermissionRequirement(EPermission.GuildLeader)))
        .AddPolicy($"{GuildPermissionPolicyPrefix}{EPermission.RankingTeam}",
            policy => policy.Requirements.Add(new GuildPermissionRequirement(EPermission.RankingTeam)))
        .AddPolicy($"{GuildPermissionPolicyPrefix}{EPermission.ScoringTeam}",
            policy => policy.Requirements.Add(new GuildPermissionRequirement(EPermission.ScoringTeam)))
        .AddPolicy($"{GuildPermissionPolicyPrefix}{EPermission.MemberTeam}",
            policy => policy.Requirements.Add(new GuildPermissionRequirement(EPermission.MemberTeam)));
}