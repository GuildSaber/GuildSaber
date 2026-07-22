using System.Net.Http.Headers;
using System.Text;

namespace GuildSaber.CSharpClient.Auth;

public abstract record GuildSaberAuthentication
{
    public sealed record CustomBasicApiKeyAuthentication(string Key, DiscordId? DiscordId) : GuildSaberAuthentication;
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
            _ => throw new ArgumentOutOfRangeException(nameof(authentication), "Unknown authentication type.")
        };
}