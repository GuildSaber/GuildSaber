using GuildSaber.Common.Helpers;
using GuildSaber.Database.Models.Server.Scores;

namespace GuildSaber.Database.Models.Mappers;

public static class ModifiersMapper
{
    private static readonly char[] _separator = [','];

    public static AbstractScore.EModifiers ToModifiers(string input) => input
        .Split(_separator, StringSplitOptions.RemoveEmptyEntries)
        .Aggregate<string, AbstractScore.EModifiers>(0, (current, modifier) => current | modifier switch
        {
            "NO" => AbstractScore.EModifiers.NoObstacles,
            "NB" => AbstractScore.EModifiers.NoBombs,
            "NF" => AbstractScore.EModifiers.NoFail,
            "SS" => AbstractScore.EModifiers.SlowerSong,
            "BE" => AbstractScore.EModifiers.BatteryEnergy,
            "IF" => AbstractScore.EModifiers.InstaFail,
            "SC" => AbstractScore.EModifiers.SmallNotes,
            "PM" => AbstractScore.EModifiers.ProMode,
            "FS" => AbstractScore.EModifiers.FasterSong,
            "SA" => AbstractScore.EModifiers.StrictAngles,
            "DA" => AbstractScore.EModifiers.DisappearingArrows,
            "GN" => AbstractScore.EModifiers.GhostNotes,
            "NA" => AbstractScore.EModifiers.NoArrows,
            "SF" => AbstractScore.EModifiers.SuperFastSong,
            "OD" => AbstractScore.EModifiers.OldDots,
            "OP" => AbstractScore.EModifiers.OffPlatform,
            _ => AbstractScore.EModifiers.Unk
        });

    public static string ToModifiersString(AbstractScore.EModifiers modifiers) => modifiers
        .GetFlags()
        .Aggregate<AbstractScore.EModifiers, string>("", (current, modifier) => current + modifier switch
        {
            AbstractScore.EModifiers.NoObstacles => "NO,",
            AbstractScore.EModifiers.NoBombs => "NB,",
            AbstractScore.EModifiers.NoFail => "NF,",
            AbstractScore.EModifiers.SlowerSong => "SS,",
            AbstractScore.EModifiers.BatteryEnergy => "BE,",
            AbstractScore.EModifiers.InstaFail => "IF,",
            AbstractScore.EModifiers.SmallNotes => "SC,",
            AbstractScore.EModifiers.ProMode => "PM,",
            AbstractScore.EModifiers.FasterSong => "FS,",
            AbstractScore.EModifiers.StrictAngles => "SA,",
            AbstractScore.EModifiers.DisappearingArrows => "DA,",
            AbstractScore.EModifiers.GhostNotes => "GN,",
            AbstractScore.EModifiers.NoArrows => "NA,",
            AbstractScore.EModifiers.SuperFastSong => "SF,",
            AbstractScore.EModifiers.OldDots => "OD,",
            AbstractScore.EModifiers.OffPlatform => "OP,",
            _ => ""
        });
}