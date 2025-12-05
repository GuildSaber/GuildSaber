using GuildSaber.Api.Features.Auth.CustomApiKey;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace GuildSaber.Api.Features.Auth.Authorization;

public static class AuthorizationExtensions
{
    private const string GuildPermissionPolicyPrefix = "GuildPermission_";

    private static readonly IList<string> _authenticationSchemes =
        [JwtBearerDefaults.AuthenticationScheme, BasicAuthenticationDefaults.AuthenticationScheme];


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
            => builder.AddPolicy(AuthConstants.ManagerPolicy, policy =>
            {
                policy.RequireRole(AuthConstants.ManagerRole);
                policy.AuthenticationSchemes = _authenticationSchemes;
            });

        public AuthorizationBuilder AddGuildAuthorizationPolicies() => builder
            .AddPolicy($"{GuildPermissionPolicyPrefix}{EPermission.GuildLeader}", policy =>
            {
                policy.Requirements.Add(new GuildPermissionRequirement(EPermission.GuildLeader));
                policy.AuthenticationSchemes = _authenticationSchemes;
            })
            .AddPolicy($"{GuildPermissionPolicyPrefix}{EPermission.RankingTeam}", policy =>
            {
                policy.Requirements.Add(new GuildPermissionRequirement(EPermission.RankingTeam));
                policy.AuthenticationSchemes = _authenticationSchemes;
            })
            .AddPolicy($"{GuildPermissionPolicyPrefix}{EPermission.ScoringTeam}", policy =>
            {
                policy.Requirements.Add(new GuildPermissionRequirement(EPermission.ScoringTeam));
                policy.AuthenticationSchemes = _authenticationSchemes;
            })
            .AddPolicy($"{GuildPermissionPolicyPrefix}{EPermission.MemberTeam}", policy =>
            {
                policy.Requirements.Add(new GuildPermissionRequirement(EPermission.MemberTeam));
                policy.AuthenticationSchemes = _authenticationSchemes;
            });
    }
}