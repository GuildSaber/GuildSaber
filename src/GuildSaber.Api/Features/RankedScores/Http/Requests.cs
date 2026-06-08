namespace GuildSaber.Api.Features.RankedScores.Http;

public static class RankedScoreRequests
{
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
