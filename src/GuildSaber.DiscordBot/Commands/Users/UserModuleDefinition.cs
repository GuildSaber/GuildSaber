using Discord;
using Discord.Interactions;
using GuildSaber.Api.Features.Guilds;
using GuildSaber.Api.Features.Guilds.Members;
using GuildSaber.Common.Result;
using GuildSaber.CSharpClient;
using GuildSaber.CSharpClient.Auth;
using GuildSaber.Database.Contexts.DiscordBot;
using GuildSaber.DiscordBot.Core.Extensions;
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
        IOptions<EmojiSettings> emojiSettings,
        HybridCache cache)
    {
        Client = new Lazy<GuildSaberClient>(() => new GuildSaberClient(
            httpClientFactory.CreateClient("GuildSaber"),
            new GuildSaberAuthentication.CustomBasicApiKeyAuthentication(
                Key: authSettings.Value.ApiKey,
                DiscordId: Context?.User?.Id.ToString() ?? string.Empty
            )));
        DbContext = dbContext;
        EmojiSettings = emojiSettings;
        Cache = cache;
    }

    private Lazy<GuildSaberClient> Client { get; }
    private DiscordBotDbContext DbContext { get; }
    private IOptions<EmojiSettings> EmojiSettings { get; }
    private HybridCache Cache { get; }

    public enum EDisplayChoice
    {
        Visible = 0,
        Invisible = 1 << 0
    }

    public async ValueTask<GuildId> GetGuildIdAsync() =>
        (await Cache.FindGuildIdFromDiscordGuildIdAsync(Context.Guild.DiscordId, Client.Value))
        .ValueOrGuildMissingException();

    public async ValueTask<GuildResponses.Guild> GetGuildAsync() =>
        (await Client.Value.Guilds.GetByIdAsync(await GetGuildIdAsync()))
        .Unwrap().ValueOrGuildMissingException();

    public async ValueTask<GuildResponses.GuildExtended> GetGuildExtendedAsync() =>
        (await Client.Value.Guilds.GetExtendedByIdAsync(await GetGuildIdAsync()))
        .Unwrap().ValueOrGuildMissingException();
}

public static class UserModuleSlashExtensions
{
    extension(UserModuleSlash.EDisplayChoice self)
    {
        public bool ToEphemeral() => self == UserModuleSlash.EDisplayChoice.Invisible;
    }
}