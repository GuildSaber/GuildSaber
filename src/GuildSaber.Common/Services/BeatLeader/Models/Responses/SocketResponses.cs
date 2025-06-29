namespace GuildSaber.Common.Services.BeatLeader.Models.Responses;

/// <summary>
/// Represents different types of score events received from the BeatLeader WebSocket.
/// </summary>
/// <remarks>
/// The BeatLeader WebSocket sends events in the format: { message: string, data: {various json objects} }
/// More message types may be added in the future as the BeatLeader API evolves.
/// </remarks>
public abstract record SocketGeneralResponse
{
    /// <summary>
    /// Represents a new score that has been uploaded but not yet processed.
    /// </summary>
    /// <param name="SocketMessage">The message containing score data.</param>
    /// <remarks>
    /// This message is sent for ANY score upload, regardless of whether it's a PB or not.
    /// For non-PB scores, this will be the only message received for that score.
    /// </remarks>
    public record Upload(SocketMessage<ScoreResponseWithMyScoreAndContexts> SocketMessage)
        : SocketGeneralResponse;

    /// <summary>
    /// Represents a score that has been processed and accepted by BeatLeader.
    /// </summary>
    /// <param name="SocketMessage">The message containing score data.</param>
    /// <remarks>
    /// This message is only sent when a score is saved to the leaderboard as a Personal Best (PB).
    /// It follows the initial 'upload' message for that score.
    /// </remarks>
    public record Accepted(SocketMessage<ScoreResponseWithMyScoreAndContexts> SocketMessage)
        : SocketGeneralResponse;

    /// <summary>
    /// Represents a score that has been processed and rejected by BeatLeader.
    /// </summary>
    /// <param name="SocketMessage">The message containing score data.</param>
    /// <remarks>
    /// This message is only sent for scores that were previously accepted but then removed from the leaderboard.
    /// Reasons for removal include: banned maps, deleted scores, or scores marked as suspicious.
    /// </remarks>
    public record Rejected(SocketMessage<ScoreResponseWithMyScoreAndContexts> SocketMessage)
        : SocketGeneralResponse;
}