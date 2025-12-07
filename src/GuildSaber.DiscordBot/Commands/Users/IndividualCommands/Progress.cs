using Discord;
using Discord.Interactions;
using GuildSaber.CSharpClient;
using GuildSaber.DiscordBot.AutocompleteHandlers;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [SlashCommand("progress", "Shows a specific player's progress, or depending on a specific category")]
    public async Task Me(
        [Summary("Category")] [Autocomplete(typeof(CategoryAutocompleteHandler))] int? categoryId = null,
        [Summary("VisibleToOther")] EDisplayChoice displayChoice = EDisplayChoice.Visible)
    {
        await DeferAsync(ephemeral: displayChoice.ToEphemeral());
        await FollowupAsync(embed: await ProgramCommand.MakeProgress(Client.Value, categoryId));
    }
}

file static class ProgramCommand
{
    public static Task<Embed> MakeProgress(GuildSaberClient client, int? categoryId)
    {
        var embedBuilder = new EmbedBuilder
        {
            Title = "Progress Command",
            Color = Color.Blue,
            Description = "This is a placeholder for the progress command."
        };

        embedBuilder.AddField("categoryId", categoryId?.ToString() ?? "None", true);

        return Task.FromResult(embedBuilder.Build());
    }
}