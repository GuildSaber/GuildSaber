using GuildSaber.Api.Features.Auth.CustomApiKey;
using GuildSaber.Api.Features.Auth.Sessions;
using Microsoft.AspNetCore.Authorization;

namespace GuildSaber.Api.Features.Auth.Authorization;

public static class AuthorizationExtensions
{
    private const string GuildPermissionPolicyPrefix = "GuildPermission_";

    private static readonly IList<string> _authenticationSchemes =
        [SessionCookieDefaults.AuthenticationScheme, BasicAuthenticationDefaults.AuthenticationScheme];

    extension(RouteHandlerBuilder builder)
    {
        public RouteHandlerBuilder WithOptionalAuthentication() => builder
            .RequireAuthorization(AuthConstants.OptionalAuthenticationPolicy);

        public RouteHandlerBuilder RequireSession() => builder
            .RequireAuthorization(AuthConstants.SessionPolicy);

        public RouteHandlerBuilder RequireManager() => builder
            .RequireAuthorization(AuthConstants.ManagerPolicy);

        public RouteHandlerBuilder RequireGuildPermission(EPermission requiredPermission) => builder
            .RequireAuthorization($"{GuildPermissionPolicyPrefix}{requiredPermission}");

        public RouteHandlerBuilder RequireAnyGuildPermission(EPermission permissions) => builder
            .RequireAuthorization(policy =>
            {
                policy.Requirements.Add(new GuildPermissionRequirement(permissions, MatchAny: true));
                policy.AuthenticationSchemes = _authenticationSchemes;
            });
    }

    extension(AuthorizationBuilder builder)
    {
        public AuthorizationBuilder AddOptionalAuthenticationPolicy() => builder
            .AddPolicy(AuthConstants.OptionalAuthenticationPolicy, policy =>
            {
                policy.AuthenticationSchemes = _authenticationSchemes;
                policy.RequireAssertion(_ => true);
            });

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