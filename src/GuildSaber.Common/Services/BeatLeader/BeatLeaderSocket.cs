using System.Buffers;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using CSharpFunctionalExtensions;
using GuildSaber.Common.Services.BeatLeader.Models;
using GuildSaber.Common.Services.BeatLeader.Models.Responses;
using Upload = GuildSaber.Common.Services.BeatLeader.BeatLeaderSocket.GeneralResponse.Upload;
using Rejected = GuildSaber.Common.Services.BeatLeader.BeatLeaderSocket.GeneralResponse.Rejected;
using Accepted = GuildSaber.Common.Services.BeatLeader.BeatLeaderSocket.GeneralResponse.Accepted;
using ConnectionError = GuildSaber.Common.Services.BeatLeader.BeatLeaderSocket.Error.ConnectionError;
using DeserializationError =
    GuildSaber.Common.Services.BeatLeader.BeatLeaderSocket.Error.DeserializationError;
using UnknownMessageTypeError =
    GuildSaber.Common.Services.BeatLeader.BeatLeaderSocket.Error.UnknownMessageTypeError;

namespace GuildSaber.Common.Services.BeatLeader;

/// <summary>
/// Provides a WebSocket connection to BeatLeader's real-time score notifications.
/// </summary>
public sealed class BeatLeaderSocket(Uri baseUri) : IAsyncDisposable
{
    /// <summary>
    /// BL's magic number for receive buffer size
    /// </summary>
    private const int BeatleaderSocketSize = 26240;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Ensures only one connection is active at a time
    /// </summary>
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    /// <summary>
    /// WebSocket endpoint for BeatLeader's general score feed
    /// </summary>
    private readonly Uri _generalUri = new(baseUri, "general");

    /// <summary>
    /// The active WebSocket connection
    /// </summary>
    private ClientWebSocket? _webSocket;

    /// <summary>
    /// Cleans up resources used by the WebSocket connection.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async ValueTask DisposeAsync()
    {
        await CleanupConnection();
        _connectionLock.Dispose();
    }

    /// <summary>
    /// Represents different types of score events received from the BeatLeader WebSocket.
    /// </summary>
    /// <remarks>
    /// The BeatLeader WebSocket sends events in the format: { message: string, data: {various json objects} }
    /// More message types may be added in the future as the BeatLeader API evolves.
    /// </remarks>
    public abstract record GeneralResponse
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
            : GeneralResponse;

        /// <summary>
        /// Represents a score that has been processed and accepted by BeatLeader.
        /// </summary>
        /// <param name="SocketMessage">The message containing score data.</param>
        /// <remarks>
        /// This message is only sent when a score is saved to the leaderboard as a Personal Best (PB).
        /// It follows the initial 'upload' message for that score.
        /// </remarks>
        public record Accepted(SocketMessage<ScoreResponseWithMyScoreAndContexts> SocketMessage)
            : GeneralResponse;

        /// <summary>
        /// Represents a score that has been processed and rejected by BeatLeader.
        /// </summary>
        /// <param name="SocketMessage">The message containing score data.</param>
        /// <remarks>
        /// This message is only sent for scores that were previously accepted but then removed from the leaderboard.
        /// Reasons for removal include: banned maps, deleted scores, or scores marked as suspicious.
        /// </remarks>
        public record Rejected(SocketMessage<ScoreResponseWithMyScoreAndContexts> SocketMessage)
            : GeneralResponse;
    }

    /// <summary>
    /// Represents different types of errors that can occur during WebSocket operations.
    /// </summary>
    public abstract record Error
    {
        /// <summary>
        /// Represents errors that occur when connecting to the WebSocket.
        /// </summary>
        /// <param name="Message">Detailed error message</param>
        public record ConnectionError(Exception Exception) : Error;

        /// <summary>
        /// Represents errors that occur when deserializing messages from the WebSocket.
        /// </summary>
        /// <param name="Message">Detailed error message</param>
        /// <param name="RawJson">The raw JSON that failed to deserialize, if available</param>
        public record DeserializationError(string Message, string? RawJson = null) : Error;

        /// <summary>
        /// Represents errors for unknown or unexpected message types from the WebSocket.
        /// </summary>
        /// <param name="MessageType">The unknown message type identifier</param>
        public record UnknownMessageTypeError(string MessageType) : Error;

        /// <summary>
        /// Represents errors that occur when the WebSocket connection is lost unexpectedly.
        /// </summary>
        /// <param name="Message">Detailed error message</param>
        /// <param name="WebSocketState">The state of the WebSocket when the error occurred</param>
        public record ConnectionLostError(string Message, WebSocketState WebSocketState) : Error;
    }

    /// <summary>
    /// Connects to BeatLeader WebSocket and streams score responses as they arrive.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// An async enumerable sequence of <see cref="Result{T,E}" /> containing
    /// <see cref="GeneralResponse" /> objects and <see cref="Error" /> on failure.
    /// </returns>
    /// <remarks>
    /// Each result will be:
    /// - Success with score data for properly parsed messages (Upload/Accepted/Rejected)
    /// - Failure with ConnectionError when connection cannot be established
    /// - Failure with DeserializationError when message parsing fails
    /// - Failure with UnknownMessageTypeError when an unknown message type is received
    /// - Failure with ConnectionLostError when the WebSocket connection is lost unexpectedly
    /// The stream will end when the WebSocket connection closes or the cancellation token is triggered.
    /// </remarks>
    public async IAsyncEnumerable<Result<GeneralResponse, Error>> StreamScoreEvents(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken);

        try
        {
            _webSocket = new ClientWebSocket();
            if ((await Try(() => _webSocket.ConnectAsync(_generalUri, cancellationToken), ex => ex))
                .TryGetError(out var connectionException))
            {
                if (connectionException is OperationCanceledException)
                    yield break;

                yield return Failure<GeneralResponse, Error>(new ConnectionError(connectionException));
                yield break;
            }

            var buffer = ArrayPool<byte>.Shared.Rent(BeatleaderSocketSize);
            try
            {
                while (_webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    if (!(await Try(
                            async () => await _webSocket.ReceiveAsync(new Memory<byte>(buffer), cancellationToken),
                            ex => ex
                        )).TryGetValue(out var webSocketResult, out var receiveError))
                    {
                        if (receiveError is OperationCanceledException)
                            yield break;

                        yield return Failure<GeneralResponse, Error>(new Error.ConnectionLostError(
                            $"WebSocket connection lost: {receiveError.Message}",
                            _webSocket.State));
                        break;
                    }

                    if (webSocketResult.MessageType == WebSocketMessageType.Close)
                        break;

                    if (webSocketResult.Count <= 0) continue;
                    var json = Encoding.UTF8.GetString(buffer, 0, webSocketResult.Count);

                    yield return Try(() => JsonSerializer
                            .Deserialize<SocketMessage<ScoreResponseWithMyScoreAndContexts>>(json, _jsonOptions)) switch
                        {
                            { IsFailure: true, Error: var deserializationError } => Failure<GeneralResponse, Error>(
                                new DeserializationError(deserializationError, json)),
                            { Value: var socketMessage } => socketMessage switch
                            {
                                null => Failure<GeneralResponse, Error>(
                                    new DeserializationError("Received null message from WebSocket", json)),
                                { Message: "upload" } =>
                                    new Upload(socketMessage),
                                { Message: "accepted" } =>
                                    new Accepted(socketMessage),
                                { Message: "rejected" } =>
                                    new Rejected(socketMessage),
                                _ => Failure<GeneralResponse, Error>(new UnknownMessageTypeError(
                                    $"Received unknown message type '{socketMessage.Message}' from WebSocket"))
                            }
                        };
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        finally
        {
            await CleanupConnection();
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// Safely closes and disposes the WebSocket connection.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task CleanupConnection()
    {
        if (_webSocket is { State: WebSocketState.Open or WebSocketState.CloseReceived })
        {
            try
            {
                using var closeTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", closeTimeoutCts.Token);
            }
            catch
            {
                // Ignore exceptions during cleanup
            }

            _webSocket.Dispose();
            _webSocket = null;
        }
    }
}