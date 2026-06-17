namespace GuildSaber.Api.Features.RankedScores.Http;

public static class RankedScoreRequests
{
    /// <summary>
    /// Filters for querying ranked scores.
    /// </summary>
    /// <param name="RankedScoreTypes">
    /// If not None, only returns selected ranked scores with any of the specified ranked score types.
    /// </param>
    /// <param name="DifficultyStarFrom">The minimum difficulty star rating to filter scores.</param>
    /// <param name="DifficultyStarTo">The maximum difficulty star rating to filter scores.</param>
    /// <param name="AccuracyStarFrom">The minimum accuracy star rating to filter scores.</param>
    /// <param name="AccuracyStarTo">The maximum accuracy star rating to filter scores.</param>
    /// <param name="BpmFrom">The minimum BPM to filter scores.</param>
    /// <param name="BpmTo">The maximum BPM to filter scores.</param>
    public record struct Filters(
        [FromQuery(Name = "rankedScoreTypes")] ERankedScoreType RankedScoreTypes = ERankedScoreType.None,
        [FromQuery(Name = "difficultyStarFrom")] float? DifficultyStarFrom = null,
        [FromQuery(Name = "difficultyStarTo")] float? DifficultyStarTo = null,
        [FromQuery(Name = "accuracyStarFrom")] float? AccuracyStarFrom = null,
        [FromQuery(Name = "accuracyStarTo")] float? AccuracyStarTo = null,
        [FromQuery(Name = "bpmFrom")] float? BpmFrom = null,
        [FromQuery(Name = "bpmTo")] float? BpmTo = null
    );

    [Flags]
    public enum ERankedScoreType
    {
        None = 0,
        Valid = 1 << 0,
        Invalid = 1 << 1,
        Pending = 1 << 2,
        Accepted = 1 << 3,
        Refused = 1 << 4,
        PointGiving = Valid | Accepted,
        NonPointGiving = Invalid | Pending | Refused,
        NonPointGivingNoPending = Invalid | Refused,
        All = Valid | Invalid | Pending | Accepted | Refused
    }

    public enum ERankedScoreSorter
    {
        Points = 0,
        DifficultyStar = 1,
        AccuracyStar = 2,
        Score = 3,
        Accuracy = 4,
        ScoreTime = 5
    }
}