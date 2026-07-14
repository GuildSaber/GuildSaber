using Discord;
using Discord.Interactions;
using GuildSaber.Api.Features.Guilds.Members.Http;
using GuildSaber.CSharpClient;
using GuildSaber.Database.Contexts.DiscordBot;
using GuildSaber.DiscordBot.Core.Extensions;
using GuildSaber.DiscordBot.Core.Handlers;
using GuildSaber.DiscordBot.Settings;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace GuildSaber.DiscordBot.Commands.Manager;

/// <summary>
/// Classes that holds as a module the definition of all interaction events.
/// Note: This class is partial because the command can only be registered in a module.
/// </summary>
/// <remarks>
/// Change <see cref="CommandContextTypeAttribute" /> and <see cref="PermissionHandler.RequirePermissionAttributeSlash" />
/// to reflect the context and permissions required for the commands to be executed in this module.
/// </remarks>
[CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
[PermissionHandler.RequirePermissionAttributeSlash(MemberResponses.EPermission.None, requireManager: true)]
public class ManagerModuleSlash : InteractionModuleBase<SocketInteractionContext>
{
    public ManagerModuleSlash(
        IHttpClientFactory httpClientFactory,
        DiscordBotDbContext dbContext,
        IOptions<AuthSettings> authSettings,
        IServiceProvider services,
        HybridCache cache)
    {
        Client = new Lazy<GuildSaberClient>(() => GuildSaberClient
            .GetAuthenticatedClient(Context?.User?.DiscordId, services));
        DbContext = dbContext;
        Cache = cache;
    }

    private Lazy<GuildSaberClient> Client { get; }
    public DiscordBotDbContext DbContext { get; }
    public HybridCache Cache { get; }
}