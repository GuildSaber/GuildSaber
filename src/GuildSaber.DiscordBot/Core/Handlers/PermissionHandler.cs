using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GuildSaber.DiscordBot.DAL;
using Microsoft.Extensions.DependencyInjection;

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
    /// Exposed enum flag used for permission management and persistance.
    /// </summary>
    [Flags]
    public enum EPermissions
    {
        None = 0,
        Manager = 1 << 0,
        Admin = 1 << 1
    }

    /// <summary>
    /// Check if the user have all the required permission flag set.
    /// </summary>
    public class RequirePermissionAttributeSlash(EPermissions permissions) : PreconditionAttribute
    {
        /// <remarks>
        /// So this is C# but with expression statements as a way to handle conditional logic.
        /// There is less chance to mess it up, but it's a bit hard to write when unused because it's unfamiliar.
        /// </remarks>
        public override Task<PreconditionResult> CheckRequirementsAsync(
            IInteractionContext context, ICommandInfo commandInfo,
            IServiceProvider services) => context.User switch
        {
            SocketUser when permissions is EPermissions.None => Success(),
            SocketUser socketUser => services.GetService<AppDbContext>() switch
            {
                null => Error("Database not found, please report the issue.", context),
                var dbContext => dbContext.Users.Find(socketUser.Id) switch
                {
                    null => Error("You might not be registered in the database.", context),
                    var user => user.Permissions.HasFlag(permissions) switch
                    {
                        true => Success(),
                        false when user.Permissions.HasFlag(EPermissions.Manager) => Success(),
                        _ => Error("You don't have the required permissions to execute this command.", context)
                    }
                }
            },
            _ => Error("You are not a valid user.", context)
        };

        private static Task<PreconditionResult> Success()
            => Task.FromResult(PreconditionResult.FromSuccess());

        private static async Task<PreconditionResult> Error(string message, IInteractionContext context)
        {
            await context.Interaction.RespondAsync(message, ephemeral: true);
            return PreconditionResult.FromError(message);
        }
    }
}
