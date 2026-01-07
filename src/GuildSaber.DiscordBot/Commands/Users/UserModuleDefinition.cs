using Discord;
using Discord.Interactions;
using GuildSaber.Api.Features.Guilds;
using GuildSaber.Api.Features.Guilds.Members;
using GuildSaber.Api.Features.Players;
using GuildSaber.Common.Result;
using GuildSaber.Common.StrongTypes;
using GuildSaber.CSharpClient;
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
[CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
[PermissionHandler.RequirePermissionAttributeSlash(MemberResponses.EPermission.None)]
public partial class UserModuleSlash : InteractionModuleBase<SocketInteractionContext>
{
    public UserModuleSlash(
        DiscordBotDbContext dbContext,
        IOptions<EmojiSettings> emojiSettings,
        IOptions<LinkSettings> linkSettings,
        HybridCache cache,
        IServiceProvider services)
    {
        Client = new Lazy<GuildSaberClient>(() => GuildSaberClient
            .GetAuthenticatedClient(Context?.User?.DiscordId, services));
        DbContext = dbContext;
        EmojiSettings = emojiSettings;
        LinkSettings = linkSettings;
        Cache = cache;
    }

    private Lazy<GuildSaberClient> Client { get; }
    private DiscordBotDbContext DbContext { get; }
    private IOptions<EmojiSettings> EmojiSettings { get; }
    private IOptions<LinkSettings> LinkSettings { get; }
    private HybridCache Cache { get; }

    public enum EDisplayChoice
    {
        [ChoiceDisplay("Visible to everyone")]
        Visible = 0,

        [ChoiceDisplay("Only you can see it")]
        Secret = 1 << 0
    }

    public async ValueTask<GuildId> GetGuildIdAsync() =>
        (await Cache.FindGuildIdFromDiscordGuildIdAsync(Context.Guild.DiscordId, Client.Value))
        .ValueOrGuildMissingException();

    public async ValueTask<PlayerId> GetPlayerId(DiscordId id) =>
        (await Client.Value.Players.LookupPlayerIdByDiscordIdAsync(id)
            .Unwrap()).ValueOrPlayerNotFoundException();

    public async ValueTask<GuildResponses.Guild> GetGuildAsync() =>
        (await Client.Value.Guilds.GetByIdAsync(await GetGuildIdAsync()))
        .Unwrap().ValueOrGuildMissingException();

    public async ValueTask<GuildResponses.GuildExtended> GetGuildExtendedAsync() =>
        (await Client.Value.Guilds.GetExtendedByIdAsync(await GetGuildIdAsync()))
        .Unwrap().ValueOrGuildMissingException();

    public async ValueTask<PlayerResponses.Player> GetPlayerAtMeAsync() =>
        (await Client.Value.Players.GetAtMeAsync())
        .UnwrapOrCurrentPlayerDidNotJoinGuildContextException()
        .ValueOrCurrentPlayerNotRegisteredException();

    public async ValueTask<PlayerResponses.Player> GetPlayerAsync(DiscordId discordId) =>
        await GetPlayerAsync(await GetPlayerId(discordId));

    public async ValueTask<PlayerResponses.Player> GetPlayerAsync(PlayerId playerId) =>
        (await Client.Value.Players.GetByIdAsync(playerId))
        .UnwrapOrPlayerDidNotJoinGuildContextException()
        .ValueOrPlayerNotFoundException();

    public async ValueTask<PlayerResponses.PlayerExtended> GetPlayerExtendedAsync(DiscordId discordId) =>
        await GetPlayerExtendedAsync(await GetPlayerId(discordId));

    public async ValueTask<PlayerResponses.PlayerExtended> GetPlayerExtendedAsync(PlayerId playerId) =>
        (await Client.Value.Players.GetExtendedByIdAsync(playerId))
        .UnwrapOrPlayerDidNotJoinGuildContextException()
        .ValueOrPlayerNotFoundException();

    public async ValueTask<PlayerResponses.PlayerExtended> GetPlayerExtendedAtMeAsync() =>
        (await Client.Value.Players.GetExtendedAtMeAsync())
        .UnwrapOrCurrentPlayerDidNotJoinGuildContextException()
        .ValueOrCurrentPlayerNotRegisteredException();
}

public static class UserModuleSlashExtensions
{
    extension(UserModuleSlash.EDisplayChoice self)
    {
        public bool ToEphemeral() => self == UserModuleSlash.EDisplayChoice.Secret;
    }
}