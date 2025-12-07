using GuildSaber.CSharpClient;
using GuildSaber.CSharpClient.Auth;
using GuildSaber.Database.Models.StrongTypes;
using GuildSaber.DiscordBot.Settings;
using Microsoft.Extensions.Options;

namespace GuildSaber.DiscordBot.Core.Extensions;

public static class GuildSaberClientExtensions
{
    extension(GuildSaberClient)
    {
        public static GuildSaberClient GetAuthenticatedClient(DiscordId discordUserId, IServiceProvider services)
            => new(
                services.GetRequiredService<IHttpClientFactory>().CreateClient("GuildSaber"),
                new GuildSaberAuthentication.CustomBasicApiKeyAuthentication(
                    Key: services.GetRequiredService<IOptions<AuthSettings>>().Value.ApiKey,
                    DiscordId: discordUserId.ToString()
                ));
    }
}