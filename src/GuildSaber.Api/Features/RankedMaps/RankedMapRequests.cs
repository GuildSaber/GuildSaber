using GuildSaber.Api.Features.RankedMaps.MapVersions;

namespace GuildSaber.Api.Features.RankedMaps;

public class RankedMapRequest
{
    [Flags]
    public enum EModifiers
    {
        None = 0,
        NoObstacles = 1 << 0,
        NoBombs = 1 << 1,
        NoFail = 1 << 2,
        SlowerSong = 1 << 3,
        BatteryEnergy = 1 << 4,
        InstaFail = 1 << 5,
        SmallNotes = 1 << 6,
        ProMode = 1 << 7,
        FasterSong = 1 << 8,
        StrictAngles = 1 << 9,
        DisappearingArrows = 1 << 10,
        GhostNotes = 1 << 11,
        NoArrows = 1 << 12,
        SuperFastSong = 1 << 13,
        OldDots = 1 << 14,
        OffPlatform = 1 << 15,
        Unk = 1 << 30,

        /// <summary>
        /// All modifiers that are commonly prohibited to giving points in ranked maps.
        /// Such as NoObstacles, NoBombs, NoFail, SlowerSong, NoArrows and OffPlatform.
        /// </summary>
        ProhibitedDefaults = NoObstacles | NoBombs | NoFail | SlowerSong | NoArrows | OffPlatform
    }

    public enum ERankedMapSorter
    {
        Id = 0,
        EditTime = 1,
        CreationTime = 2,
        DifficultyStar = 3,
        AccuracyStar = 4
    }

    /// <param name="DifficultyStar">
    /// When specified, the difficulty rating of the map won't be calculated by BeatLeader's ExMachina.
    /// </param>
    /// <param name="AccuracyStar">
    /// When specified, the accuracy rating of the map won't be calculated by BeatLeader's ExMachina.
    /// </param>
    public record ManualRating(float? DifficultyStar = null, float? AccuracyStar = null);

    public record RankedMapRequirements(
        bool NeedConfirmation = false,
        bool NeedFullCombo = false,
        float? MaxPauseDurationSec = null,
        EModifiers ProhibitedModifiers = EModifiers.ProhibitedDefaults,
        EModifiers MandatoryModifiers = EModifiers.None,
        float? MinAccuracy = null
    );

    /// <param name="BaseMapVersion">
    /// The map version that will be used as the base for the ranked map.
    /// </param>
    /// <param name="Requirements"></param>
    /// <param name="ManualRating">
    /// Used to force a specific difficulty and/or accuracy star rating for the map.
    /// If null, the star ratings will be calculated automatically by BeatLeader's ExMachina.
    /// </param>
    /// <param name="CategoryIds">
    /// The categories to assign the ranked map to.
    /// </param>
    /// <param name="LevelIds">
    /// The levels to assign the ranked map to (in case of RankedMapList levels).
    /// </param>
    /// <remarks>
    /// If you want to create a ranked map with multiple map versions, create the ranked map with one map version first,
    /// then add the other map versions using the proper endpoint.
    /// </remarks>
    public record CreateRankedMap(
        MapVersionRequests.AddMapVersion BaseMapVersion,
        RankedMapRequirements Requirements,
        ManualRating ManualRating,
        int[] CategoryIds,
        int[]? LevelIds
    );
}