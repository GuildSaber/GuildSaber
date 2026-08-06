using System.Net;

namespace GuildSaber.Api.Features.Auth;

internal static class OriginPolicy
{
    public static bool IsAllowed(string? url, IEnumerable<string> configuredOrigins)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        if (IsLocalOrigin(uri))
            return true;

        var origin = uri.GetLeftPart(UriPartial.Authority);

        return configuredOrigins.Any(configuredOrigin =>
            Uri.TryCreate(configuredOrigin, UriKind.Absolute, out var configuredUri) &&
            string.Equals(configuredUri.GetLeftPart(UriPartial.Authority), origin,
                StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsLocalOrigin(Uri uri)
    {
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return false;

        if (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
            return true;

        if (!IPAddress.TryParse(uri.Host.Trim('[', ']'), out var address))
            return false;

        return IPAddress.IsLoopback(address) || address.GetAddressBytes() is [192, 168, _, _];
    }
}
