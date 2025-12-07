using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GuildSaber.Api.Features.Guilds.Members;
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
        /// <remarks>
        /// So this is C# but with expression statements as a way to handle conditional logic.
        /// There is less chance to mess it up, but it's a bit hard to write when unused because it's unfamiliar.
        /// </remarks>
        public override async Task<PreconditionResult> CheckRequirementsAsync(
            IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
            => context switch
            {
                _ when !requireManager && permissions == MemberResponses.EPermission.None => Succeed(),
                { User: SocketUser user } => (await services.GetRequiredService<HybridCache>()
                        .GetUserPermissionsOnDiscordGuildsAsync(user.DiscordId, services))
                    .TryGetValue(context.Guild.DiscordId, out var value) switch
                    {
                        _ when requireManager && !value.IsManager
                            => await Error("You must be a guild manager to execute this command.", context),
                        _ when value.Permissions.HasFlag(permissions) => Succeed(),
                        _ when value.IsManager => Succeed(),
                        _ => await Error("You don't have the required permissions to execute this command.", context)
                    },
                _ => await Error("You are not a valid user.", context)
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
        private static PreconditionResult Succeed()
            => PreconditionResult.FromSuccess();
    }
}