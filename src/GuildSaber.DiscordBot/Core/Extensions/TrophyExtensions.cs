using GuildSaber.Common.StrongTypes;
using GuildSaber.DiscordBot.Settings;

namespace GuildSaber.DiscordBot.Core.Extensions;

public static class TrophyExtensions
{
    extension(Trophy)
    {
        public static Trophy? GetFromPercentage(double percentage) => percentage switch
        {
            0 => null,
            <= 0.25f => Trophy.Plastic,
            <= 0.50f => Trophy.Silver,
            <= 0.75 => Trophy.Gold,
            < 1 => Trophy.Diamond,
            _ => Trophy.Ruby
        };
    }

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