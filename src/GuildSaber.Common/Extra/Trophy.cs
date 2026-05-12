namespace GuildSaber.Common.Extra;

public record struct TrophiesData(
    int Plastic,
    int Silver,
    int Gold,
    int Diamond,
    int Ruby
);

/// <summary>
/// Represents the trophy abstraction in use but never sent directly over the wire.
/// </summary>
public enum Trophy
{
    Plastic = 0,
    Silver = 1,
    Gold = 2,
    Diamond = 3,
    Ruby = 4
}

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
}