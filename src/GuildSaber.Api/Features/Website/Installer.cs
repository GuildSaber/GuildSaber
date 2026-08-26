using GuildSaber.Api.Features.Website.LinkPreviews;

namespace GuildSaber.Api.Features.Website;

public static class WebsiteInstaller
{
    public static IServiceCollection AddWebsiteFeature(this IServiceCollection services)
        => services.AddScoped<RankedMapLinkPreviewService>();

    public static IApplicationBuilder UseWebsiteFeature(this IApplicationBuilder app) => app
        .UseMiddleware<LinkPreviewMiddleware>();
}