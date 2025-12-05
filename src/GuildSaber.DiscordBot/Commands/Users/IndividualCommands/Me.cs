using Discord;
using Discord.Interactions;
using GuildSaber.Common.Helpers;
using GuildSaber.Common.Result;
using GuildSaber.CSharpClient;

namespace GuildSaber.DiscordBot.Commands.Users;

/// <remarks>
/// This class is partial because the command can only be registered in a module.
/// </remarks>
public partial class UserModuleSlash
{
    [SlashCommand("me", "Get information about your account")]
    public async Task Me([Summary("VisibleToOther")] EDisplayChoice displayChoice = EDisplayChoice.Invisible)
    {
        await DeferAsync(ephemeral: displayChoice.ToEphemeral());
        await FollowupAsync(embed: await MeCommand.GetAtMe(Client.Value));
    }
}

/// <summary>
/// Static class used to hold functions for the current command.
/// </summary>
file static class MeCommand
{
    public static async Task<Embed> GetAtMe(GuildSaberClient client)
    {
        var embedBuilder = new EmbedBuilder();

        var atMe = await client.Players.GetExtendedAtMeAsync(CancellationToken.None)
            .Unwrap();

        embedBuilder.Title = $"{atMe.Player.PlayerInfo.Username}'s Profile";
        embedBuilder.Color = Color.Blue;
        embedBuilder.AddField("Player ID", atMe.Player.Id, true);
        embedBuilder.AddField("Username", atMe.Player.PlayerInfo.Username, true);
        // add a field to see if the user is a manager
        embedBuilder.AddField("Is Manager", atMe.Player.IsManager);

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