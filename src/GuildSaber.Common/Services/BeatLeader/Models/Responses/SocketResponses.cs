using System.Text.Json.Serialization;
using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;

namespace GuildSaber.Common.Services.BeatLeader.Models.Responses;

/// <summary>
/// Represents a general socket message received from the BeatLeader WebSocket.
/// This base record is used for polymorphic deserialization of different score-related events.
/// For specific event types, see the derived records of <see cref="GeneralSocketMessage{T}" />.
/// </summary>
/// <param name="BeatLeaderId">The BeatLeader ID of the player associated with the score event.</param>
/// <param name="LeaderboardId">The ID of the leaderboard associated with the score event.</param>
/// <remarks>
/// The BeatLeader WebSocket sends events in the format: { message: string, data: {various json objects} }
/// Depending on the 'message' field, the 'data' field will contain different types of score-related information.
/// </remarks>
[JsonDerivedType(typeof(GeneralSocketMessage<UploadedScore>), "upload")]
[JsonDerivedType(typeof(GeneralSocketMessage<AcceptedScore>), "accepted")]
[JsonDerivedType(typeof(GeneralSocketMessage<RejectedScore>), "rejected")]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "message")]
public abstract record GeneralSocketMessage(BeatLeaderId BeatLeaderId, string LeaderboardId);

/// <summary>
/// Represents different types of score events received from the BeatLeader WebSocket.
/// </summary>
public record GeneralSocketMessage<T>(T Data) : GeneralSocketMessage(Data.PlayerId, Data.LeaderboardId)
    where T : IUnprocessedScore
{
    public required T Data { get; init; } = Data;
}

/// <summary>
/// Represents a new score that has been uploaded but not yet processed.
/// </summary>
/// <remarks>
/// This represent ANY score upload, regardless of whether it's a PB or not.
/// For non-PB scores, this will be the only score event received for that score.
/// </remarks>
public record UploadedScore : IUnprocessedScore
{
    public required BeatLeaderId PlayerId { get; init; }
    public required BLLeaderboardId LeaderboardId { get; init; }

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

/// <summary>
/// Represents a score that has been processed and accepted by BeatLeader.
/// </summary>
/// <remarks>
/// This only represent a score when it is saved to the leaderboard as a Personal Best (PB).
/// It follows the initial 'upload' message for that score.
/// </remarks>
public record AcceptedScore : IProcessedScore, IWithScoreContext, IWithAcc
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

    public required BeatLeaderId PlayerId { get; init; }
    public required BLLeaderboardId LeaderboardId { get; init; }

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

/// <summary>
/// Represents a score that has been processed and rejected by BeatLeader.
/// </summary>
/// <remarks>
/// This only represent scores that were previously accepted but then removed from the leaderboard.
/// Reasons for removal include: banned maps, deleted scores, or scores marked as suspicious.
/// </remarks>
public record RejectedScore : IProcessedScore, IWithScoreContext, IWithAcc
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

    public required BeatLeaderId PlayerId { get; init; }
    public required BLLeaderboardId LeaderboardId { get; init; }

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