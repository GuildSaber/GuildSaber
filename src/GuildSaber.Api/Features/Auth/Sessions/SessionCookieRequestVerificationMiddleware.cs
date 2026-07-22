using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Primitives;

namespace GuildSaber.Api.Features.Auth.Sessions;

public sealed class SessionCookieRequestVerificationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (RequiresVerification(context) && !HasValidVerificationHeader(context.Request))
        {
            await TypedResults.Problem(
                    "A valid GuildSaber request verification header is required.",
                    statusCode: StatusCodes.Status403Forbidden)
                .ExecuteAsync(context);
            return;
        }

        await next(context);
    }

    private static bool RequiresVerification(HttpContext context)
    {
        if (HttpMethods.IsGet(context.Request.Method) ||
            HttpMethods.IsHead(context.Request.Method) ||
            HttpMethods.IsOptions(context.Request.Method) ||
            HttpMethods.IsTrace(context.Request.Method))
            return false;

        if (context.GetEndpoint()?.Metadata.GetMetadata<IAuthorizeData>() is null)
            return false;

        return context.User.Identities.Any(identity =>
            identity.IsAuthenticated &&
            string.Equals(identity.AuthenticationType, SessionCookieDefaults.AuthenticationScheme,
                StringComparison.Ordinal));
    }

    private static bool HasValidVerificationHeader(HttpRequest request)
        => request.Headers.TryGetValue(SessionCookieDefaults.RequestVerificationHeaderName, out var value) &&
           StringValues.Equals(value, SessionCookieDefaults.RequestVerificationHeaderValue);
}