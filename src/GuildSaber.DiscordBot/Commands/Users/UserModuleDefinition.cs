using Discord;
using Discord.Interactions;
using GuildSaber.CSharpClient;
using GuildSaber.CSharpClient.Auth;
using GuildSaber.Database.Contexts.DiscordBot;
using GuildSaber.Database.Models.DiscordBot;
using GuildSaber.DiscordBot.Auth;
using GuildSaber.DiscordBot.Core.Handlers;
using Microsoft.Extensions.Options;

namespace GuildSaber.DiscordBot.Commands.Users;

/// <summary>
/// Classes that holds as a module the definition of all interaction events.
/// Note: This class is partial because the command can only be registered in a module.
/// </summary>
/// <remarks>
/// Change <see cref="CommandContextTypeAttribute" /> and <see cref="PermissionHandler.RequirePermissionAttributeSlash" />
/// to reflect the context and permissions required for the commands to be executed in this module.
/// </remarks>
[CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel, InteractionContextType.BotDm)]
[PermissionHandler.RequirePermissionAttributeSlash(User.EPermissions.None)]
public partial class UserModuleSlash(
    GuildSaberClient client,
#pragma warning disable CS9113 // Parameter is unread.
    DiscordBotDbContext dbContext,
#pragma warning restore CS9113 // Parameter is unread.
    IOptions<AuthSettings> authSettings)
    : InteractionModuleBase<SocketInteractionContext>
{
    public GuildSaberAuthentication CurrentUserAuth => new GuildSaberAuthentication.CustomBasicApiKeyAuthentication(
        Key: authSettings.Value.ApiKey,
        DiscordId: Context.User.Id.ToString()
    );
}