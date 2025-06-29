using System.Net.WebSockets;
using System.Text.Json;
using CSharpFunctionalExtensions;
using GuildSaber.Common.Services.BeatLeader.Models;
using GuildSaber.Common.Services.BeatLeader.Models.Responses;
using Error = GuildSaber.Common.Services.BeatLeader.Errors.ClientWebSocketStreamError;
using static GuildSaber.Common.Services.BeatLeader.Errors.ClientWebSocketStreamError;
using static GuildSaber.Common.Services.BeatLeader.Models.Responses.SocketGeneralResponse;

namespace GuildSaber.Common.Services.BeatLeader;

/// <summary>
/// Provides a WebSocket stream for receiving real-time general updates from the BeatLeader API.
/// This class establishes a connection to the BeatLeader general socket and yields parsed messages
/// as they are received.
/// </summary>
/// <param name="baseUri">The base URI of the BeatLeader WebSocket endpoint.</param>
/// <remarks>
/// The stream automatically handles WebSocket connection management, message buffering, and
/// deserialization of incoming JSON messages. It supports message types including score uploads,
/// acceptances, and rejections.
/// The class implements both <see cref="IDisposable" /> and <see cref="IAsyncDisposable" /> for
/// proper resource cleanup and can only be enumerated once at a time.
/// </remarks>
public sealed class BeatLeaderGeneralSocketStream(Uri baseUri) : IAsyncEnumerable<Result<SocketGeneralResponse, Error>>,
    IDisposable,
    IAsyncDisposable
{
    private const int FrameLength = 1024;
    private const int MaxMessageLength = 5 * 1024 * 1024;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly Uri _generalUri = new(baseUri, "general");

    private readonly byte[] _receiveBuffer = new byte[MaxMessageLength];
    private int _receiveBufferWPos;
    private ClientWebSocket? _webSocket;

    internal WebSocketState State => _webSocket?.State ?? WebSocketState.None;

    /// <summary>
    /// Asynchronously disposes of the WebSocket connection and associated resources.
    /// </summary>
    /// <returns>A <see cref="ValueTask" /> representing the asynchronous disposal operation.</returns>
    public async ValueTask DisposeAsync() => await CleanupWebSocketAsync();

    /// <summary>
    /// Returns an asynchronous enumerator that iterates through the WebSocket messages.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the enumeration.</param>
    /// <returns>
    /// An async enumerator that yields <see cref="Result{T, TError}" /> containing either:
    /// - Success with a <see cref="SocketGeneralResponse" /> for valid messages
    /// - Failure with an <see cref="Error" /> for connection issues, deserialization failures, or protocol errors
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when the stream is already being enumerated.</exception>
    /// <remarks>
    /// The enumerator will:
    /// - Establish a WebSocket connection to the BeatLeader general endpoint
    /// - Continuously receive and parse JSON messages until cancellation or connection loss
    /// - Automatically handle connection cleanup on completion or error
    /// - Stop enumeration on cancellation, connection errors, or unknown message types
    /// Supported message types are: "upload", "accepted", and "rejected".
    /// Messages exceeding 5MB will result in a <see cref="Error.MessageTooLong" /> error.
    /// </remarks>
    public async IAsyncEnumerator<Result<SocketGeneralResponse, Error>> GetAsyncEnumerator(
        CancellationToken cancellationToken = new())
    {
        if (StreamAlreadyInUse())
            throw new InvalidOperationException("Stream is already in use.");

        _webSocket = new ClientWebSocket();

        var connectResult = await TryConnectWebSocket(_webSocket, _generalUri, cancellationToken);
        try
        {
            if (connectResult.TryGetError(out var connectionException))
            {
                if (connectionException is not OperationCanceledException)
                    yield return Failure<SocketGeneralResponse, Error>(new ConnectionError(connectionException));

                yield break;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                if (_webSocket.State != WebSocketState.Open)
                {
                    yield return Failure<SocketGeneralResponse, Error>(
                        new ConnectionLost(_webSocket.State));

                    yield break;
                }

                var idealSize = Math.Min(FrameLength, _receiveBuffer.Length - _receiveBufferWPos);
                var receiveArraySegment = new ArraySegment<byte>(_receiveBuffer, _receiveBufferWPos, idealSize);

                if (!(await TryReceiveAsync(_webSocket, receiveArraySegment, cancellationToken).ConfigureAwait(false))
                    .TryGetValue(out var received, out var receiveException))
                {
                    if (receiveException is not OperationCanceledException)
                        yield return Failure<SocketGeneralResponse, Error>(new ConnectionError(receiveException));

                    yield break;
                }

                if (received.MessageType is not WebSocketMessageType.Text)
                {
                    yield return Failure<SocketGeneralResponse, Error>(
                        new UnknownMessageType($"Received unsupported message type: {received.MessageType}"));

                    yield break;
                }

                if (!TryIncrementByChecked(ref _receiveBufferWPos, received.Count)
                    || _receiveBufferWPos > MaxMessageLength)
                {
                    yield return Failure<SocketGeneralResponse, Error>(
                        new MessageTooLong("Received message exceeds maximum length of 5 MB"));

                    yield break;
                }

                // If the message is not complete, continue receiving
                if (!received.EndOfMessage) continue;

                // Performance assumption that the message is in UTF-8 format (in this case, it should be)
                var mem = _receiveBuffer.AsMemory(0, _receiveBufferWPos);
                if (!TryDeserializeMessage<SocketMessage<ScoreResponseWithMyScoreAndContexts>>(mem.Span, _jsonOptions)
                        .TryGetValue(out var message, out var deserializationError))
                {
                    yield return Failure<SocketGeneralResponse, Error>(deserializationError);
                    continue;
                }

                yield return message switch
                {
                    { Message: "upload" } => new Upload(message),
                    { Message: "accepted" } => new Accepted(message),
                    { Message: "rejected" } => new Rejected(message),
                    _ => Failure<SocketGeneralResponse, Error>(
                        new UnknownMessageType($"Received unknown message type '{message.Message}'"))
                };

                _receiveBufferWPos = 0;
            }
        }
        finally
        {
            await CleanupWebSocketAsync();
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="BeatLeaderGeneralSocketStream" />.
    /// </summary>
    /// <remarks>
    /// This method disposes the WebSocket connection synchronously and resets internal state.
    /// For asynchronous disposal with proper connection closure, use <see cref="DisposeAsync" />.
    /// </remarks>
    public void Dispose()
    {
        _webSocket?.Dispose();
        _webSocket = null;
        _receiveBufferWPos = 0;
    }

    #region Private Helper Methods

    /// <summary>
    /// Safely attempts to receive data from the WebSocket with exception handling.
    /// </summary>
    /// <param name="webSocket">The WebSocket to receive from.</param>
    /// <param name="buffer">The buffer to store received data.</param>
    /// <param name="cancellationToken">A cancellation token for the receive operation.</param>
    /// <returns>
    /// A result containing either the <see cref="WebSocketReceiveResult" /> on success,
    /// or the <see cref="Exception" /> that occurred during the receive operation.
    /// </returns>
    private async ValueTask<Result<WebSocketReceiveResult, Exception>> TryReceiveAsync(
        ClientWebSocket webSocket, ArraySegment<byte> buffer, CancellationToken cancellationToken)
    {
        try
        {
            return Success<WebSocketReceiveResult, Exception>(await webSocket.ReceiveAsync(buffer, cancellationToken)
                .ConfigureAwait(false));
        }
        catch (Exception ex)
        {
            return Failure<WebSocketReceiveResult, Exception>(ex);
        }
    }

    private bool TryIncrementByChecked(ref int value, int increment)
    {
        try
        {
            checked
            {
                value += increment;
            }

            return true;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    private static Result<T?, DeserializationError> TryDeserializeMessage<T>(
        ReadOnlySpan<byte> utf8Json,
        JsonSerializerOptions? options = null)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(utf8Json, options);
        }
        catch (JsonException ex)
        {
            return Failure<T?, DeserializationError>(
                new DeserializationError(ex, utf8Json.ToString()));
        }
    }

    private static async ValueTask<UnitResult<Exception>> TryConnectWebSocket(
        ClientWebSocket clientWebSocket, Uri uri, CancellationToken cancellationToken)
    {
        try
        {
            await clientWebSocket.ConnectAsync(uri, cancellationToken);
            return UnitResult.Success<Exception>();
        }
        catch (Exception ex)
        {
            return Failure(ex);
        }
    }

    private async Task CleanupWebSocketAsync()
    {
        if (_webSocket is { State: WebSocketState.Open or WebSocketState.CloseReceived })
        {
            try
            {
                using var closeTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                await _webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Closing connection",
                    closeTimeoutCts.Token);
            }
            catch
            {
                // Ignore exceptions during cleanup
            }
        }

        _webSocket?.Dispose();
        _webSocket = null;
        _receiveBufferWPos = 0;
    }

    private bool StreamAlreadyInUse() => _webSocket is not null;

    #endregion
}