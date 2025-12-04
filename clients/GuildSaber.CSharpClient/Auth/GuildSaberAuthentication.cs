using System.Net.Http.Headers;
using System.Text;

namespace GuildSaber.CSharpClient.Auth;

public abstract record GuildSaberAuthentication
{
    public sealed record CustomBasicApiKeyAuthentication(string Key, string DiscordId) : GuildSaberAuthentication;
    public sealed record BearerAuthentication(string Token) : GuildSaberAuthentication;
}

public static class GuildSaberAuthenticationExtensions
{
    public static AuthenticationHeaderValue ToAuthenticationHeader(this GuildSaberAuthentication authentication)
        => authentication switch
        {
            GuildSaberAuthentication.CustomBasicApiKeyAuthentication apiKeyAuthentication => new
                AuthenticationHeaderValue(
                    "Basic",
                    Convert.ToBase64String(Encoding.UTF8.GetBytes(
                        $"{apiKeyAuthentication.DiscordId}:{apiKeyAuthentication.Key}"))),
            GuildSaberAuthentication.BearerAuthentication bearerAuthentication
                => new AuthenticationHeaderValue("Bearer", bearerAuthentication.Token),
            _ => throw new ArgumentOutOfRangeException(nameof(authentication), "Unknown authentication type.")
        };
}