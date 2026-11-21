using Discord;
using Discord.Interactions;
using GuildSaber.Api.Features.Guilds.Members.Http;
using GuildSaber.CSharpClient;
using GuildSaber.DiscordBot.Core.Extensions;
using GuildSaber.DiscordBot.Core.Handlers;
using Microsoft.Extensions.Caching.Hybrid;

namespace GuildSaber.DiscordBot.Commands.GuildLeader;

[CommandContextType(InteractionContextType.Guild)]
[PermissionHandler.RequirePermissionAttributeSlash(MemberResponses.EPermission.GuildLeader)]
public partial class GuildLeaderModuleSlash : InteractionModuleBase<SocketInteractionContext>
{
    public GuildLeaderModuleSlash(IHttpClientFactory httpClientFactory, HybridCache cache, IServiceProvider services)
    {
        HttpClientFactory = httpClientFactory;
        Cache = cache;
        Client = new Lazy<GuildSaberClient>(() => GuildSaberClient
            .GetAuthenticatedClient(Context?.User?.DiscordId, services));
    }

    private IHttpClientFactory HttpClientFactory { get; }
    private HybridCache Cache { get; }
    private Lazy<GuildSaberClient> Client { get; }

    private async ValueTask<GuildId> GetGuildIdAsync() =>
        (await Cache.FindGuildIdFromDiscordGuildIdAsync(Context.Guild.DiscordId, Client.Value))
        .ValueOrGuildMissingException();
}
