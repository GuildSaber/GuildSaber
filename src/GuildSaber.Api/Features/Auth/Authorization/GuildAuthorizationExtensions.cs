using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Members;

namespace GuildSaber.Api.Features.Auth.Authorization;

public static class GuildAuthorizationExtensions
{
    public const string GuildIdRouteKey = "guildId";
    private const string GuildPermissionPolicyPrefix = "GuildPermission_";

    public static IServiceCollection AddGuildAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy($"{GuildPermissionPolicyPrefix}{Member.EPermission.GuildLeader}",
                policy => policy.Requirements.Add(new GuildPermissionRequirement(Member.EPermission.GuildLeader)))
            .AddPolicy($"{GuildPermissionPolicyPrefix}{Member.EPermission.RankingTeam}",
                policy => policy.Requirements.Add(new GuildPermissionRequirement(Member.EPermission.RankingTeam)))
            .AddPolicy($"{GuildPermissionPolicyPrefix}{Member.EPermission.ScoringTeam}",
                policy => policy.Requirements.Add(new GuildPermissionRequirement(Member.EPermission.ScoringTeam)))
            .AddPolicy($"{GuildPermissionPolicyPrefix}{Member.EPermission.MemberTeam}",
                policy => policy.Requirements.Add(new GuildPermissionRequirement(Member.EPermission.MemberTeam)))
            .AddPolicy($"{GuildPermissionPolicyPrefix}{Member.EPermission.GuildSaberManager}",
                policy => policy.Requirements.Add(
                    new GuildPermissionRequirement(Member.EPermission.GuildSaberManager)));

        return services;
    }

    public static RouteHandlerBuilder RequireGuildPermission(
        this RouteHandlerBuilder builder, Member.EPermission requiredPermission)
        => builder.RequireAuthorization(policyNames: $"{GuildPermissionPolicyPrefix}{requiredPermission}")
            .AddEndpointFilter(async (context, next) =>
            {
                if (!context.HttpContext.Request.RouteValues.TryGetValue(GuildIdRouteKey, out var guildIdString)
                    || guildIdString is not string guildIdStr
                    || !Guild.GuildId.TryParse(guildIdStr, out _))
                    return Results.BadRequest($"Missing or invalid route parameter: {GuildIdRouteKey}");

                return await next(context);
            });
}