using Microsoft.AspNetCore.Authorization;

namespace GuildSaber.Api.Features.Auth.Authorization;

public static class AuthorizationExtensions
{
    private const string GuildPermissionPolicyPrefix = "GuildPermission_";

    extension(RouteHandlerBuilder builder)
    {
        public RouteHandlerBuilder RequireManager() => builder
            .RequireAuthorization(AuthConstants.ManagerPolicy);

        public RouteHandlerBuilder RequireGuildPermission(EPermission requiredPermission) => builder
            .RequireAuthorization($"{GuildPermissionPolicyPrefix}{requiredPermission}");
    }

    extension(AuthorizationBuilder builder)
    {
        public AuthorizationBuilder AddManagerAuthorizationPolicy()
            => builder.AddPolicy(AuthConstants.ManagerPolicy, policy => policy.RequireRole(AuthConstants.ManagerRole));

        public AuthorizationBuilder AddGuildAuthorizationPolicies() => builder
            .AddPolicy($"{GuildPermissionPolicyPrefix}{EPermission.GuildLeader}",
                policy => policy.Requirements.Add(new GuildPermissionRequirement(EPermission.GuildLeader)))
            .AddPolicy($"{GuildPermissionPolicyPrefix}{EPermission.RankingTeam}",
                policy => policy.Requirements.Add(new GuildPermissionRequirement(EPermission.RankingTeam)))
            .AddPolicy($"{GuildPermissionPolicyPrefix}{EPermission.ScoringTeam}",
                policy => policy.Requirements.Add(new GuildPermissionRequirement(EPermission.ScoringTeam)))
            .AddPolicy($"{GuildPermissionPolicyPrefix}{EPermission.MemberTeam}",
                policy => policy.Requirements.Add(new GuildPermissionRequirement(EPermission.MemberTeam)));
    }
}