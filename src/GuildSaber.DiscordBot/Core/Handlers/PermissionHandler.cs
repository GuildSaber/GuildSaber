using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GuildSaber.Api.Features.Guilds.Members;
using GuildSaber.Common.Result;
using GuildSaber.CSharpClient;
using GuildSaber.CSharpClient.Auth;
using GuildSaber.DiscordBot.Settings;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace GuildSaber.DiscordBot.Core.Handlers;

/// <summary>
/// Specifies the permission required to execute a command.
/// </summary>
/// <remarks>
/// This is where you add the various handlers you want to be executed to verify a command is allowed execution.
/// </remarks>
public static class PermissionHandler
{
    /// <summary>
    /// Check if the user have all the required permission flag set.
    /// </summary>
    public class RequirePermissionAttributeSlash(MemberResponses.EPermission permissions, bool requireManager = false)
        : PreconditionAttribute
    {
        public readonly record struct MemberPermissionWithIsManager(
            MemberResponses.EPermission Permissions,
            bool IsManager
        );

        /// <remarks>
        /// So this is C# but with expression statements as a way to handle conditional logic.
        /// There is less chance to mess it up, but it's a bit hard to write when unused because it's unfamiliar.
        /// </remarks>
        public override async Task<PreconditionResult> CheckRequirementsAsync(
            IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
            => context switch
            {
                _ when !requireManager && permissions == MemberResponses.EPermission.None => Succeed(),
                { User: SocketUser user } => (await GetUserPermissionsOnDiscordGuildsAsync(
                        user.Id, services, services.GetRequiredService<HybridCache>()))
                    .TryGetValue(context.Guild.Id, out var value) switch
                    {
                        _ when requireManager && !value.IsManager
                            => await Error("You must be a guild manager to execute this command.", context),
                        _ when value.Permissions.HasFlag(permissions) => Succeed(),
                        _ when value.IsManager => Succeed(),
                        _ => await Error("You don't have the required permissions to execute this command.", context)
                    },
                _ => await Error("You are not a valid user.", context)
            };

        internal static ValueTask<ulong?> FindDiscordGuildIdFromGuildId(
            int guildId, GuildSaberClient client, HybridCache cache)
            => cache.GetOrCreateAsync($"GuildDiscordId_{guildId}", (guildId, client),
                async static (state, token) =>
                {
                    var guildResult = await state.client.Guilds.GetByIdAsync(state.guildId, token)
                        .Unwrap();

                    if (guildResult?.DiscordInfo.MainDiscordGuildId is null)
                        return null;

                    if (!ulong.TryParse(guildResult.DiscordInfo.MainDiscordGuildId, out var discordGuildId))
                        throw new InvalidOperationException(
                            $"Guild {state.guildId} has an invalid Discord Guild ID: {guildResult.DiscordInfo.MainDiscordGuildId}");

                    return discordGuildId as ulong?;
                },
                new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromHours(1)
                });

        private static GuildSaberClient GetAuthenticatedClient(ulong discordUserId, IServiceProvider services)
        {
            var httpClientFactory = services.GetRequiredService<IHttpClientFactory>();
            var authSettings = services.GetRequiredService<IOptions<AuthSettings>>();

            return new GuildSaberClient(
                httpClientFactory.CreateClient("GuildSaber"),
                new GuildSaberAuthentication.CustomBasicApiKeyAuthentication(
                    Key: authSettings.Value.ApiKey,
                    DiscordId: discordUserId.ToString()
                ));
        }

        /// <summary>
        /// Get the permissions of a user by their Discord User ID.
        /// </summary>
        /// <returns>A dictionary mapping Discord Guild IDs to the user's permissions in those guilds.</returns>
        internal static ValueTask<Dictionary<ulong, MemberPermissionWithIsManager>>
            GetUserPermissionsOnDiscordGuildsAsync(ulong id, IServiceProvider services, HybridCache cache)
            => services.GetRequiredService<HybridCache>().GetOrCreateAsync($"DiscordUserPermissions_{id}",
                (services, id, cache),
                async static (state, token) =>
                {
                    var client = GetAuthenticatedClient(state.id, state.services);
                    var response = await client.Players.GetExtendedAtMeAsync(token)
                        .Unwrap();

                    if (response is not { Player: var player, Members: var members })
                        return new Dictionary<ulong, MemberPermissionWithIsManager>();

                    var permissionsByGuild = new Dictionary<ulong, MemberPermissionWithIsManager>();
                    foreach (var member in members)
                    {
                        var discordGuildId = await FindDiscordGuildIdFromGuildId(member.GuildId, client, state.cache);
                        if (discordGuildId is null) continue;

                        permissionsByGuild.Add(
                            discordGuildId.Value,
                            new MemberPermissionWithIsManager(member.Permissions, player.IsManager)
                        );
                    }

                    return permissionsByGuild;
                },
                new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromMinutes(20)
                });

        private static PreconditionResult Succeed()
            => PreconditionResult.FromSuccess();

        private static async Task<PreconditionResult> Error(string message, IInteractionContext context)
        {
            await (context.Interaction.HasResponded
                ? context.Interaction.FollowupAsync(message, ephemeral: true)
                : context.Interaction.RespondAsync(message, ephemeral: true));

            return PreconditionResult.FromError(message);
        }
    }
}