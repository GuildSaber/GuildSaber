using GuildSaber.Common.Helpers;
using ERankedScoreType = GuildSaber.Api.Features.RankedScores.Http.RankedScoreRequests.ERankedScoreType;

namespace GuildSaber.Api.Features.RankedMaps.Http;

public static class RankedMapRequests
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
    /// When <paramref name="RankedScoreTypes" /> is specified, if true, the maps without scores will be included, and for
    /// those with scores the <paramref name="RankedScoreTypes" /> filter will be applied.
    /// When false, the filter only applies when <paramref name="RankedScoreTypes" /> is specified (and not None).
    /// </param>
    /// <param name="DifficultyStarFrom">The minimum difficulty star rating to filter maps.</param>
    /// <param name="DifficultyStarTo">The maximum difficulty star rating to filter maps.</param>
    /// <param name="AccuracyStarFrom">The minimum accuracy star rating to filter maps.</param>
    /// <param name="AccuracyStarTo">The maximum accuracy star rating to filter maps.</param>
    /// <param name="DurationSecFrom">The minimum duration (in seconds) to filter maps.</param>
    /// <param name="DurationSecTo">The maximum duration (in seconds) to filter maps.</param>
    /// <param name="BpmFrom">The minimum BPM to filter maps.</param>
    /// <param name="BpmTo">The maximum BPM to filter maps.</param>
    /// <param name="NeedConfirmation">
    /// If specified, filters maps based on whether they require confirmation for ranked scores.
    /// </param>
    public record struct Filters(
        [property: FromQuery(Name = "search")] string? Search = null,
        [property: FromQuery(Name = "categoryIds")] int[]? CategoryIds = null,
        [property: FromQuery(Name = "matchAnyCategory")] bool MatchAnyCategory = true,
        [property: FromQuery(Name = "rankedScoreTypes")] ERankedScoreType RankedScoreTypes = ERankedScoreType.None,
        [property: FromQuery(Name = "includeMapsWithoutScore")] bool IncludeMapsWithoutScore = false,
        [property: FromQuery(Name = "difficultyStarFrom")] float? DifficultyStarFrom = null,
        [property: FromQuery(Name = "difficultyStarTo")] float? DifficultyStarTo = null,
        [property: FromQuery(Name = "accuracyStarFrom")] float? AccuracyStarFrom = null,
        [property: FromQuery(Name = "accuracyStarTo")] float? AccuracyStarTo = null,
        [property: FromQuery(Name = "durationSecFrom")] float? DurationSecFrom = null,
        [property: FromQuery(Name = "durationSecTo")] float? DurationSecTo = null,
        [property: FromQuery(Name = "bpmFrom")] float? BpmFrom = null,
        [property: FromQuery(Name = "bpmTo")] float? BpmTo = null,
        [property: FromQuery(Name = "needConfirmation")] bool? NeedConfirmation = null
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
        RankTime = 2,
        MapUploadTime = 3,
        RankedScoreTime = 4,
        DifficultyStar = 5,
        AccuracyStar = 6,
        Name = 7
    }

    /// <summary>
    /// Optional manual star ratings. Missing values are resolved before a proposal can be submitted.
    /// </summary>
    /// <param name="DifficultyStar">
    /// Finite, non-negative difficulty stars. Null or omitted requests automatic calculation from BeatLeader's
    /// ExMachina at submission, using the lowest-order proposed difficulty and the proposed requirements.
    /// </param>
    /// <param name="AccuracyStar">
    /// Finite, non-negative accuracy stars. Null or omitted requests calculation at submission from BeatLeader's
    /// ExMachina and the guild's accuracy curve. Missing or invalid calculation data prevents submission.
    /// </param>
    public record ManualRating(float? DifficultyStar = null, float? AccuracyStar = null);

    /// <summary>
    /// Requirements for a score to qualify on the ranked map.
    /// </summary>
    /// <param name="NeedConfirmation">Otherwise eligible scores remain pending until explicitly confirmed.</param>
    /// <param name="NeedFullCombo">Require a full-combo score.</param>
    /// <param name="MaxPauseDurationSec">
    /// Maximum total pause duration in seconds. Must be finite and non-negative. Null imposes no pause limit.
    /// Setting a limit excludes ScoreSaber scores, which lack the required pause tracking.
    /// </param>
    /// <param name="ProhibitedModifiers">
    /// Any matching modifier invalidates the score. Must not overlap mandatory modifiers.
    /// </param>
    /// <param name="MandatoryModifiers">
    /// Every selected modifier must be present. Must not overlap prohibited modifiers.
    /// </param>
    /// <param name="MinAccuracy">Minimum accuracy in percent, from 0 to 100 inclusive. Null imposes no minimum.</param>
    public record RankedMapRequirements(
        bool NeedConfirmation = false,
        bool NeedFullCombo = false,
        float? MaxPauseDurationSec = null,
        EModifiers ProhibitedModifiers = EModifiers.ProhibitedDefaults,
        EModifiers MandatoryModifiers = EModifiers.None,
        float? MinAccuracy = null
    );

    public static string ToModifiersString(this EModifiers modifiers) => modifiers
        .GetFlags()
        .Aggregate<EModifiers, string>("", (current, modifier) => current + modifier switch
        {
            EModifiers.NoObstacles => "NO,",
            EModifiers.NoBombs => "NB,",
            EModifiers.NoFail => "NF,",
            EModifiers.SlowerSong => "SS,",
            EModifiers.BatteryEnergy => "BE,",
            EModifiers.InstaFail => "IF,",
            EModifiers.SmallNotes => "SC,",
            EModifiers.ProMode => "PM,",
            EModifiers.FasterSong => "FS,",
            EModifiers.StrictAngles => "SA,",
            EModifiers.DisappearingArrows => "DA,",
            EModifiers.GhostNotes => "GN,",
            EModifiers.NoArrows => "NA,",
            EModifiers.SuperFastSong => "SF,",
            EModifiers.OldDots => "OD,",
            EModifiers.OffPlatform => "OP,",
            _ => ""
        });
}