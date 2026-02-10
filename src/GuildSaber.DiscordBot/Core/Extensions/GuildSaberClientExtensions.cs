using GuildSaber.Common.Settings;
using GuildSaber.Common.StrongTypes;
using GuildSaber.CSharpClient;
using GuildSaber.CSharpClient.Auth;
using GuildSaber.DiscordBot.Settings;
using Microsoft.Extensions.Options;

namespace GuildSaber.DiscordBot.Core.Extensions;

public static class GuildSaberClientExtensions
{
    extension(GuildSaberClient)
    {
        public static GuildSaberClient GetAuthenticatedClient(DiscordId? discordUserId, IServiceProvider services)
            => new(
                httpClient: services.GetRequiredService<IHttpClientFactory>().CreateClient("GuildSaber"),
                cdnBaseUri: services.GetRequiredService<IOptions<LinkSettings>>().Value.CdnBaseUri,
                new GuildSaberAuthentication.CustomBasicApiKeyAuthentication(
                    Key: services.GetRequiredService<IOptions<AuthSettings>>().Value.ApiKey,
                    DiscordId: discordUserId
                ));

        public static GuildSaberClient GetNonAuthenticatedClient(IServiceProvider services) => new(
            httpClient: services.GetRequiredService<IHttpClientFactory>().CreateClient("GuildSaber"),
            cdnBaseUri: services.GetRequiredService<IOptions<LinkSettings>>().Value.CdnBaseUri,
            authentication: null);
    }
}