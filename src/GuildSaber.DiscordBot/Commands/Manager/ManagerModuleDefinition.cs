using Discord;
using Discord.Interactions;
using GuildSaber.CSharpClient;
using GuildSaber.CSharpClient.Auth;
using GuildSaber.Database.Contexts.DiscordBot;
using GuildSaber.Database.Models.DiscordBot;
using GuildSaber.DiscordBot.Auth;
using GuildSaber.DiscordBot.Core.Handlers;
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
[CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel, InteractionContextType.BotDm)]
[PermissionHandler.RequirePermissionAttributeSlash(User.EPermissions.Manager)]
public partial class ManagerModuleSlash : InteractionModuleBase<SocketInteractionContext>
{
    public ManagerModuleSlash(
        IHttpClientFactory httpClientFactory,
        DiscordBotDbContext dbContext,
        IOptions<AuthSettings> authSettings)
    {
        Client = new Lazy<GuildSaberClient>(() => new GuildSaberClient(
            httpClientFactory.CreateClient("GuildSaber"),
            new GuildSaberAuthentication.CustomBasicApiKeyAuthentication(
                Key: authSettings.Value.ApiKey,
                DiscordId: Context?.User?.Id.ToString() ?? string.Empty
            )));
        DbContext = dbContext;
    }

    private Lazy<GuildSaberClient> Client { get; }
    public DiscordBotDbContext DbContext { get; }
}