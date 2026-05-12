using GuildSaber.Common.Extra;
using GuildSaber.DiscordBot.Settings;

namespace GuildSaber.DiscordBot.Core.Extensions;

public static class TrophyExtensions
{
    public static (Trophy trophy, string emoji)? GetFromPercentage(this TrophyEmojis trophyEmojis, double percentage)
        => Trophy.GetFromPercentage(percentage) switch
        {
            null => null,
            var trophy => trophy.Value switch
            {
                Trophy.Plastic => (Trophy.Plastic, trophyEmojis.Plastic),
                Trophy.Silver => (Trophy.Silver, trophyEmojis.Silver),
                Trophy.Gold => (Trophy.Gold, trophyEmojis.Gold),
                Trophy.Diamond => (Trophy.Diamond, trophyEmojis.Diamond),
                Trophy.Ruby => (Trophy.Ruby, trophyEmojis.Ruby),
                _ => throw new ArgumentOutOfRangeException()
            }
        };
}