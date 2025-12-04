using Discord;
using Discord.Interactions;
using GuildSaber.Common.Helpers;
using GuildSaber.Common.Result;
using GuildSaber.CSharpClient;
using GuildSaber.CSharpClient.Auth;

namespace GuildSaber.DiscordBot.Commands.Users;

/// <remarks>
/// This class is partial because the command can only be registered in a module.
/// </remarks>
public partial class UserModuleSlash
{
    [SlashCommand("me", "Get information about your account")]
    public async Task Me() => await RespondAsync(embed: await MeCommand.GetAtMeButCrash(client, CurrentUserAuth));
}

/// <summary>
/// Static class used to hold functions for the current command.
/// </summary>
file static class MeCommand
{
    public static async Task<Embed> GetAtMeButCrash(GuildSaberClient client, GuildSaberAuthentication authentication)
    {
        var embedBuilder = new EmbedBuilder();

        var atMe = await client.Players.GetAtMeAsync(authentication, CancellationToken.None)
            .Unwrap();

        embedBuilder.Title = $"{atMe.Player.PlayerInfo.Username}'s Profile";
        embedBuilder.Color = Color.Blue;
        embedBuilder.AddField("Player ID", atMe.Player.Id, true);
        embedBuilder.AddField("Username", atMe.Player.PlayerInfo.Username, true);

        foreach (var member in atMe.Members)
        {
            var guildId = member.GuildId;
            var permissionFlag = member.Permissions;

            var guild = await client.Guilds
                .GetByIdAsync(guildId, CancellationToken.None)
                .Unwrap();
            if (guild is null) continue;

            embedBuilder.AddField(
                $"{guild.Info.Name} (ID: {guild.Id})",
                $"Permissions: {permissionFlag
                    .GetFlags()
                    .Aggregate("", (current, flag) => current + flag + ", ")}");
        }

        return embedBuilder.Build();
    }
}