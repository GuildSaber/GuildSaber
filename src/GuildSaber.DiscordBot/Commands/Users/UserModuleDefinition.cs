using Discord;
using Discord.Interactions;
using GuildSaber.Api.Features.Guilds.Members;
using GuildSaber.CSharpClient;
using GuildSaber.CSharpClient.Auth;
using GuildSaber.Database.Contexts.DiscordBot;
using GuildSaber.DiscordBot.Core.Handlers;
using GuildSaber.DiscordBot.Settings;
using Microsoft.Extensions.Caching.Hybrid;
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
[PermissionHandler.RequirePermissionAttributeSlash(MemberResponses.EPermission.None)]
public partial class UserModuleSlash : InteractionModuleBase<SocketInteractionContext>
{
    public UserModuleSlash(
        IHttpClientFactory httpClientFactory,
        DiscordBotDbContext dbContext,
        IOptions<AuthSettings> authSettings,
        HybridCache cache)
    {
        Client = new Lazy<GuildSaberClient>(() => new GuildSaberClient(
            httpClientFactory.CreateClient("GuildSaber"),
            new GuildSaberAuthentication.CustomBasicApiKeyAuthentication(
                Key: authSettings.Value.ApiKey,
                DiscordId: Context?.User?.Id.ToString() ?? string.Empty
            )));
        DbContext = dbContext;
        Cache = cache;
    }

    private Lazy<GuildSaberClient> Client { get; }
    public DiscordBotDbContext DbContext { get; }
    public HybridCache Cache { get; }

    public enum EDisplayChoice
    {
        Visible = 0,
        Invisible = 1 << 0
    }
}

public static class UserModuleSlashExtensions
{
    extension(UserModuleSlash.EDisplayChoice self)
    {
        public bool ToEphemeral() => self == UserModuleSlash.EDisplayChoice.Invisible;
    }
}