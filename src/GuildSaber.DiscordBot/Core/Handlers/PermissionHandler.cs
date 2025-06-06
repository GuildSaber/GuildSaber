﻿using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GuildSaber.Database.Contexts.DiscordBot;
using GuildSaber.Database.Models.DiscordBot;

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
    public class RequirePermissionAttributeSlash(User.EPermissions permissions) : PreconditionAttribute
    {
        /// <remarks>
        /// So this is C# but with expression statements as a way to handle conditional logic.
        /// There is less chance to mess it up, but it's a bit hard to write when unused because it's unfamiliar.
        /// </remarks>
        public override async Task<PreconditionResult> CheckRequirementsAsync(
            IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
            => context.User switch
            {
                SocketUser when permissions is User.EPermissions.None => Success(),
                SocketUser socketUser => services.GetService<DiscordBotDbContext>() switch
                {
                    null => await Error("Database not found, please report the issue.", context),
                    var dbContext => await dbContext.Users.FindAsync(socketUser.Id) switch
                    {
                        null => await Error("You might not be registered in the database.", context),
                        var user => user.Permissions.HasFlag(permissions) switch
                        {
                            true => Success(),
                            false when user.Permissions.HasFlag(User.EPermissions.Manager) => Success(),
                            _ => await Error("You don't have the required permissions to execute this command.",
                                context)
                        }
                    }
                },
                _ => await Error("You are not a valid user.", context)
            };

        private static PreconditionResult Success()
            => PreconditionResult.FromSuccess();

        private static async Task<PreconditionResult> Error(string message, IInteractionContext context)
        {
            await context.Interaction.RespondAsync(message, ephemeral: true);
            return PreconditionResult.FromError(message);
        }
    }
}