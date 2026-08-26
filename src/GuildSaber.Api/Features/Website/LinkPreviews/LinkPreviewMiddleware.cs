using System.Text;
using GuildSaber.Common.Settings;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using MyCSharp.HttpUserAgentParser;
using MyCSharp.HttpUserAgentParser.AspNetCore;

namespace GuildSaber.Api.Features.Website.LinkPreviews;

internal sealed class LinkPreviewMiddleware(
    RequestDelegate next,
    IHttpUserAgentParserAccessor userAgentParser,
    IOptions<LinkSettings> linkSettings)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsSupportedMethod(context.Request.Method) ||
            !TryGetRankedMapId(context.Request.Path, out var rankedMapId))
        {
            await next(context);
            return;
        }

        context.Response.Headers.Append(HeaderNames.Vary, HeaderNames.UserAgent);

        if (userAgentParser.Get(context) is not { } userAgent
            || !userAgent.IsRobot()
            && !userAgent.UserAgent.Contains("bot", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var rankedMapPreviewService = context.RequestServices.GetRequiredService<RankedMapLinkPreviewService>();
        var preview = await rankedMapPreviewService.GetAsync(rankedMapId, context.RequestAborted);
        if (preview is null)
        {
            await next(context);
            return;
        }

        var pageUrl = GetPageUrl(context.Request, linkSettings.Value.WebsiteBaseUri, rankedMapId);
        var html = RankedMapLinkPreviewRenderer.Render(preview, pageUrl);

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.ContentLength = Encoding.UTF8.GetByteCount(html);
        context.Response.Headers.CacheControl = $"public, max-age={RankedMapLinkPreviewService.CacheDurationSeconds}";

        if (HttpMethods.IsGet(context.Request.Method))
            await context.Response.WriteAsync(html, context.RequestAborted);
    }

    internal static bool TryGetRankedMapId(PathString requestPath, out RankedMapId rankedMapId)
    {
        rankedMapId = default;
        var path = requestPath.Value;
        if (string.IsNullOrEmpty(path))
            return false;

        if (path.StartsWith("/website/", StringComparison.OrdinalIgnoreCase))
            path = path["/website".Length..];

        const string rankedMapPath = "/maps/";
        if (!path.StartsWith(rankedMapPath, StringComparison.OrdinalIgnoreCase))
            return false;

        var id = path.AsSpan(rankedMapPath.Length);
        if (id.EndsWith('/'))
            id = id[..^1];

        return !id.IsEmpty &&
               !id.Contains('/') &&
               RankedMapId.TryParse(id.ToString(), out rankedMapId) &&
               rankedMapId.Value > 0;
    }

    private static bool IsSupportedMethod(string method)
        => HttpMethods.IsGet(method) || HttpMethods.IsHead(method);

    private static string GetPageUrl(HttpRequest request, Uri websiteBaseUri, RankedMapId rankedMapId)
        => websiteBaseUri.IsAbsoluteUri
            ? $"{websiteBaseUri.AbsoluteUri.TrimEnd('/')}/maps/{rankedMapId}"
            : request.GetDisplayUrl();
}