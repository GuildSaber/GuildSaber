using GuildSaber.Api.Features.RankedMaps.MapVersions.Http;
using ERankedScoreType = GuildSaber.Api.Features.RankedScores.Http.RankedScoreRequests.ERankedScoreType;

namespace GuildSaber.Api.Features.RankedMaps.Http;

public class RankedMapRequests
{
    /// <summary>
    /// Filters for querying ranked maps.
    /// </summary>
    /// <param name="Search">
    /// A search term to filter the returned ranked maps. The search is applied on the 'Song.Name',
    /// 'Song.MapperName', 'Song.SongAuthorName', 'Song.BeatSaverKey', and 'Song.Hash' fields.
    /// </param>
    /// <param name="CategoryIds">
    /// An array of category IDs to filter the returned ranked maps. Only maps with at least one of the provided categories
    /// will be returned.
    /// </param>
    /// <param name="MatchAnyCategory">
    /// If true, returns maps matching any of the provided categories. If false, returns only maps matching all provided
    /// categories.
    /// </param>
    /// <param name="RankedScoreTypes">
    /// If not None, only returns maps where the player has at least one selected ranked score with any of the specified
    /// ranked score types.
    /// </param>
    /// <param name="IncludeMapsWithoutScore">
    /// If true, also returns maps where the player has no selected ranked score.
    /// </param>
    /// <param name="DifficultyStarFrom">The minimum difficulty star rating to filter maps.</param>
    /// <param name="DifficultyStarTo">The maximum difficulty star rating to filter maps.</param>
    /// <param name="AccuracyStarFrom">The minimum accuracy star rating to filter maps.</param>
    /// <param name="AccuracyStarTo">The maximum accuracy star rating to filter maps.</param>
    /// <param name="DurationSecFrom">The minimum duration (in seconds) to filter maps.</param>
    /// <param name="DurationSecTo">The maximum duration (in seconds) to filter maps.</param>
    /// <param name="BpmFrom">The minimum BPM to filter maps.</param>
    /// <param name="BpmTo">The maximum BPM to filter maps.</param>
    /// <param name="NeedConfirmation">If specified, filters maps based on whether they require confirmation for ranked scores.</param>
    public record struct Filters(
        [FromQuery(Name = "search")] string? Search = null,
        [FromQuery(Name = "categoryIds")] int[]? CategoryIds = null,
        [FromQuery(Name = "matchAnyCategory")] bool MatchAnyCategory = true,
        [FromQuery(Name = "rankedScoreTypes")] ERankedScoreType RankedScoreTypes = ERankedScoreType.None,
        [FromQuery(Name = "includeMapsWithoutScore")] bool IncludeMapsWithoutScore = false,
        [FromQuery(Name = "difficultyStarFrom")] float? DifficultyStarFrom = null,
        [FromQuery(Name = "difficultyStarTo")] float? DifficultyStarTo = null,
        [FromQuery(Name = "accuracyStarFrom")] float? AccuracyStarFrom = null,
        [FromQuery(Name = "accuracyStarTo")] float? AccuracyStarTo = null,
        [FromQuery(Name = "durationSecFrom")] float? DurationSecFrom = null,
        [FromQuery(Name = "durationSecTo")] float? DurationSecTo = null,
        [FromQuery(Name = "bpmFrom")] float? BpmFrom = null,
        [FromQuery(Name = "bpmTo")] float? BpmTo = null,
        [FromQuery(Name = "needConfirmation")] bool? NeedConfirmation = null
    );

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
        AccuracyStar = 4,
        Name = 5,
        RankedScoreTime = 6
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
        int[] LevelIds
    );

    public record UpdateRankedMap(
        RankedMapRequirements Requirements,
        ManualRating ManualRating,
        int[] CategoryIds,
        int[] LevelIds
    );
}
