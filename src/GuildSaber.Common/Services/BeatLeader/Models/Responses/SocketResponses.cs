using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;

namespace GuildSaber.Common.Services.BeatLeader.Models.Responses;

/// <summary>
/// Represents different types of score events received from the BeatLeader WebSocket.
/// </summary>
/// <remarks>
/// The BeatLeader WebSocket sends events in the format: { message: string, data: {various json objects} }
/// Depending on the 'message' field, the 'data' field will contain different types of score-related information.
/// </remarks>
public abstract record SocketGeneralResponse(string BeatLeaderId, string LeaderboardId)
{
    /// <summary>
    /// Represents a new score that has been uploaded but not yet processed.
    /// </summary>
    /// <param name="Score">The score that has been uploaded.</param>
    /// <remarks>
    /// This message is sent for ANY score upload, regardless of whether it's a PB or not.
    /// For non-PB scores, this will be the only message received for that score.
    /// </remarks>
    public record Upload(UploadScoreResponse Score)
        : SocketGeneralResponse(Score.PlayerId, Score.LeaderboardId);

    /// <summary>
    /// Represents a score that has been processed and accepted by BeatLeader.
    /// </summary>
    /// <param name="Score">The score that has been accepted.</param>
    /// <remarks>
    /// This message is only sent when a score is saved to the leaderboard as a Personal Best (PB).
    /// It follows the initial 'upload' message for that score.
    /// </remarks>
    public record Accepted(AcceptedScoreResponse Score)
        : SocketGeneralResponse(Score.PlayerId, Score.LeaderboardId);

    /// <summary>
    /// Represents a score that has been processed and rejected by BeatLeader.
    /// </summary>
    /// <param name="Score">The score that has been rejected.</param>
    /// <remarks>
    /// This message is only sent for scores that were previously accepted but then removed from the leaderboard.
    /// Reasons for removal include: banned maps, deleted scores, or scores marked as suspicious.
    /// </remarks>
    public record Rejected(RejectedScoreResponse Score)
        : SocketGeneralResponse(Score.PlayerId, Score.LeaderboardId);
}

public record UploadScoreResponse : IUnprocessedScore
{
    public required string PlayerId { get; init; }
    public required string LeaderboardId { get; init; }

    public required int BaseScore { get; init; }
    public required int ModifiedScore { get; init; }
    public required float Accuracy { get; init; }
    public required int BadCuts { get; init; }
    public required int BombCuts { get; init; }
    public required int MissedNotes { get; init; }
    public required int WallsHit { get; init; }
    public required string Modifiers { get; init; }
    public required int Pauses { get; init; }
    public required bool FullCombo { get; init; }
    public required HMD Hmd { get; init; }
    public required ControllerEnum Controller { get; init; }
    public required string Platform { get; init; }

    public required string TimeSet { get; init; }
    public required int TimePost { get; init; }
}

public record AcceptedScoreResponse : IProcessedScore, IWithScoreContext, IWithAcc
{
    public required float Pp { get; init; }
    public required BeatLeaderScoreId Id { get; init; }
    public required int Rank { get; init; }
    public required string Country { get; init; }
    public required float FcAccuracy { get; init; }
    public required int MaxCombo { get; init; }
    public required int? MaxStreak { get; init; }
    public required string Replay { get; init; }
    public required ReplayOffsets Offsets { get; init; }

    public required string PlayerId { get; init; }
    public required string LeaderboardId { get; init; }

    public required int BaseScore { get; init; }
    public required int ModifiedScore { get; init; }
    public required float Accuracy { get; init; }
    public required int BadCuts { get; init; }
    public required int BombCuts { get; init; }
    public required int MissedNotes { get; init; }
    public required int WallsHit { get; init; }
    public required string Modifiers { get; init; }
    public required int Pauses { get; init; }
    public required bool FullCombo { get; init; }
    public required HMD Hmd { get; init; }
    public required ControllerEnum Controller { get; init; }
    public required string Platform { get; init; }

    public required string TimeSet { get; init; }
    public required int TimePost { get; init; }

    public required float AccLeft { get; init; }
    public required float AccRight { get; init; }

    public required LeaderboardContexts ValidContexts { get; init; }
    public required ICollection<ScoreContextExtensionResponse> ContextExtensions { get; init; }
}

public record RejectedScoreResponse : IProcessedScore, IWithScoreContext, IWithAcc
{
    public required float Pp { get; init; }
    public required BeatLeaderScoreId Id { get; init; }
    public required int Rank { get; init; }
    public required string Country { get; init; }
    public required float FcAccuracy { get; init; }
    public required int MaxCombo { get; init; }
    public required int? MaxStreak { get; init; }
    public required string Replay { get; init; }
    public required ReplayOffsets Offsets { get; init; }

    public required string PlayerId { get; init; }
    public required string LeaderboardId { get; init; }

    public required int BaseScore { get; init; }
    public required int ModifiedScore { get; init; }
    public required float Accuracy { get; init; }
    public required int BadCuts { get; init; }
    public required int BombCuts { get; init; }
    public required int MissedNotes { get; init; }
    public required int WallsHit { get; init; }
    public required string Modifiers { get; init; }
    public required int Pauses { get; init; }
    public required bool FullCombo { get; init; }
    public required HMD Hmd { get; init; }
    public required ControllerEnum Controller { get; init; }
    public required string Platform { get; init; }

    public required string TimeSet { get; init; }
    public required int TimePost { get; init; }

    public required float AccLeft { get; init; }
    public required float AccRight { get; init; }

    public required LeaderboardContexts ValidContexts { get; init; }
    public required ICollection<ScoreContextExtensionResponse> ContextExtensions { get; init; }
}