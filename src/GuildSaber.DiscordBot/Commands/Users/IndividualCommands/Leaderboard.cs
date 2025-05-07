using Discord;
using Discord.Interactions;

namespace GuildSaber.DiscordBot.Commands.Users;

/// <remarks>
/// This class is partial because the command can only be registered in a module.
/// </remarks>
public partial class UserModuleSlash
{
    [SlashCommand("addscore", "Adds a score to the leaderboard")]
    public async Task AddScore([Summary("score", "The score to add")] decimal score)
    {
        LeaderboardCommand.Scores.Add(score);
        await RespondAsync($"Score {score} added!");
    }

    [SlashCommand("leaderboard", "Displays the leaderboard")]
    public async Task Leaderboard()
    {
        var embed = LeaderboardCommand.CreateLeaderboardEmbed(LeaderboardCommand.Scores);
        await RespondAsync(embed: embed);
    }
}

/// <summary>
/// Static class used to hold functions for the current file.
/// </summary>
file static class LeaderboardCommand
{
    public static readonly List<decimal> Scores = [100.2m, 60m, 75.5m, 88.0m, 92.3m];

    public static Embed CreateLeaderboardEmbed(List<decimal> scores)
    {
        var embedBuilder = new EmbedBuilder()
            .WithTitle("Leaderboard")
            .WithColor(Color.Green);

        if (scores.Count == 0)
        {
            embedBuilder.Description = "No scores available.";
            return embedBuilder.Build();
        }

        // Sort scores and create leaderboard entries
        var sortedScores = scores
            .Select((score, index) => new { Position = index + 1, Score = score })
            .OrderByDescending(x => x.Score)
            .ToList();

        // Build the leaderboard description
        embedBuilder.Description = string.Join(
            "\n",
            sortedScores.Select(x => $"{x.Position}. Score: {x.Score}")
        );

        return embedBuilder.Build();
    }
}