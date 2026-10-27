using GuildSaber.Api.Features.MapRequests.Services;

namespace GuildSaber.Api.Features.MapRequests;

public static class MapRequestsInstaller
{
    public static IServiceCollection AddMapRequestsFeature(this IServiceCollection services) => services
        .AddScoped<MapProposalService>()
        .AddScoped<MapProposalDraftService>()
        .AddScoped<MapProposalReviewService>()
        .AddScoped<MapProposalVoteService>()
        .AddScoped<MapRequestQueries>()
        .AddScoped<MapRequestBoostService>();
}