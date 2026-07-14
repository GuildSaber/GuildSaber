using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GuildSaber.Api.Features.Guilds.Members.Http;
using GuildSaber.CSharpClient;
using GuildSaber.DiscordBot.Core.Extensions;
using Microsoft.Extensions.Caching.Hybrid;

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
        private HybridCache? _cache;

        private HybridCache GetCachedCache(IServiceProvider services)
            => _cache ??= services.GetRequiredService<HybridCache>();

        /// <remarks>
        /// So this is C# but with expression statements as a way to handle conditional logic.
        /// There is less chance to mess it up, but it's a bit hard to write when unused because it's unfamiliar.
        /// </remarks>
        public override async Task<PreconditionResult> CheckRequirementsAsync(
            IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
            => context switch
            {
                _ when !requireManager && permissions == MemberResponses.EPermission.None
                    => await ValidateGuildIdAndContextIdOwnership(context.Interaction.Data, services, context, true),
                { User: SocketUser user } => await GetCachedCache(services)
                        .GetUserPermissionsOnDiscordGuildsAsync(user.DiscordId, services) switch
                    {
                        { IsManager: true } => Success(),
                        _ when requireManager
                            => await Error("You must be a guild manager to execute this command.", context),
                        {
                                DiscordGuildPermissions:
                                var userPermFromDiscordGuild
                            }
                            when userPermFromDiscordGuild.TryGetValue(context.Guild.DiscordId, out var permission)
                                 && permission.HasFlag(permissions)
                            => await ValidateGuildIdAndContextIdOwnership(context.Interaction.Data, services, context,
                                false),
                        _ => await Error("You don't have the required permissions to execute this command.", context)
                    },
                _ => await Error("You are not a valid user.", context)
            };

        private async Task<PreconditionResult> ValidateGuildIdAndContextIdOwnership(
            IDiscordInteractionData interactionData, IServiceProvider services, IInteractionContext context,
            bool bypassIfMissingGuild)
        {
            if (!TryGetOwnershipOptions(interactionData, out var hasOwnershipOptions, out var guildId,
                    out var contextId))
                return await Error(
                    "You attempted to execute this command with an invalid guild and/or context.",
                    context);

            /*Command doesn't listen to user inputting the guild or context id, considered owned.*/
            if (!hasOwnershipOptions)
                return Success();

            if (context.Guild is null)
                return await Error(
                    "You attempted to execute this command with another guild and/or context that's not tied to this discord server.",
                    context);

            if (await VerifyGuildIdAndContextIdOwnership(guildId, contextId, services, context.Guild.DiscordId,
                    bypassIfMissingGuild))
                return Success();

            return await Error(
                "You attempted to execute this command with another guild and/or context that's not tied to this discord server.",
                context);
        }

        private async Task<bool> VerifyGuildIdAndContextIdOwnership(
            int? guildId, int? contextId, IServiceProvider services, DiscordGuildId discordGuildId,
            bool bypassIfMissingGuild)
        {
            var cache = GetCachedCache(services);
            var client = GuildSaberClient.GetNonAuthenticatedClient(services);

            /* We get the current guild from this DiscordGuildId and we check the user inputs against it.*/
            var currentGuildId = (await cache.FindGuildIdFromDiscordGuildIdAsync(discordGuildId, client))
                .GetValueOrDefault();

            if (currentGuildId == default)
                /* For commands that doesn't require specific perms, we might allow commands to run even though the guild is missing
                 * It's not like the website or the api can't display those info anyway. */
                return bypassIfMissingGuild;

            var guildExtended = await cache.GetGuildExtendedAsync(currentGuildId, client);
            if (guildExtended is null)
                return false;

            /*User inputted a GuildId that's not the current guild, considered not owned.*/
            if (guildId is not null && guildId.Value != currentGuildId.Value)
                return false;

            var contextIdFromGuild = guildExtended.Contexts.FirstOrDefault(c => c.Id.Value == contextId).Id;

            /*If user inputted a ContextId that doesn't belong to the current guild, considered not owned.*/
            return contextId is null || contextIdFromGuild != default;
        }

        private static bool TryGetOwnershipOptions(
            IDiscordInteractionData interactionData, out bool hasOwnershipOptions, out int? guildId, out int? contextId)
        {
            guildId = null;
            contextId = null;

            var options = interactionData switch
            {
                SocketSlashCommandData data => data.Options,
                SocketMessageComponentData => null,
                _ => throw new UnauthorizedAccessException("Unknown Interaction Data type")
            };

            var guildIdOption = options?.FirstOrDefault(x => x.Name == "guild-id");
            var contextIdOption = options?.FirstOrDefault(x => x.Name is "context-id" or "context");

            hasOwnershipOptions = guildIdOption is not null || contextIdOption is not null;
            if (!hasOwnershipOptions)
                return true;

            guildId = GetIntOption(guildIdOption);
            contextId = GetIntOption(contextIdOption);

            return (guildIdOption is null || guildId is not null)
                   && (contextIdOption is null || contextId is not null);
        }

        private static int? GetIntOption(IApplicationCommandInteractionDataOption? option) => option?.Value switch
        {
            null => null,
            int intValue => intValue,
            long longValue and >= int.MinValue and <= int.MaxValue => (int)longValue,
            { } rawValue when int.TryParse(rawValue.ToString(), out var parsedValue) => parsedValue,
            _ => null
        };

        /// <summary>
        /// Send an error message to the user and return a failed precondition result.
        /// </summary>
        private static async Task<PreconditionResult> Error(string message, IInteractionContext context)
        {
            await (context.Interaction.HasResponded
                ? context.Interaction.FollowupAsync(message, ephemeral: true)
                : context.Interaction.RespondAsync(message, ephemeral: true));

            return PreconditionResult.FromError(message);
        }

        /// <summary>
        /// Return a successful precondition result.
        /// </summary>
        private static PreconditionResult Success()
            => PreconditionResult.FromSuccess();
    }
}