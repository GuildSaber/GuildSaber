using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using CSharpFunctionalExtensions;
using GuildSaber.Common.Services.BeatLeader.Models.Responses;
using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using Error = GuildSaber.Common.Services.BeatLeader.Errors.ClientWebSocketStreamError;
using static GuildSaber.Common.Services.BeatLeader.Errors.ClientWebSocketStreamError;

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
public sealed class BeatLeaderGeneralSocketStream(Uri baseUri) : IAsyncEnumerable<Result<GeneralSocketMessage, Error>>,
    IDisposable,
    IAsyncDisposable
{
    private const int FrameLength = 1024;
    private const int MaxMessageLength = 5 * 1024 * 1024;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new BeatLeaderIdJsonConverter(), new BeatLeaderScoreIdJsonConverter() }
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
    /// - Success with a <see cref="GeneralSocketMessage" /> for valid messages
    /// - Failure with an <see cref="Error" /> for connection issues, deserialization failures, or protocol errors
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when the stream is already being enumerated.</exception>
    /// <remarks>
    /// The enumerator will:
    /// <list type="bullet">
    ///     <item>Establish a WebSocket connection to the BeatLeader general endpoint</item>
    ///     <item>Continuously receive and parse JSON messages until cancellation or connection loss</item>
    ///     <item>Automatically handle connection cleanup on completion or error</item>
    ///     <item>Stop enumeration on cancellation, connection errors, or unknown message types</item>
    /// </list>
    /// Supported message types are: "upload", "accepted", and "rejected".
    /// Messages exceeding 5MB will result in a <see cref="Error.MessageTooLong" /> error.
    /// </remarks>
    public async IAsyncEnumerator<Result<GeneralSocketMessage, Error>> GetAsyncEnumerator(
        CancellationToken cancellationToken = new())
    {
        if (StreamAlreadyInUse())
            throw new InvalidOperationException("Stream is already in use.");

        _webSocket = new ClientWebSocket();
        _receiveBufferWPos = 0;

        var connectResult = await TryConnectWebSocket(_webSocket, _generalUri, cancellationToken);
        try
        {
            if (connectResult.TryGetError(out var connectionException))
            {
                if (connectionException is not OperationCanceledException)
                    yield return Failure<GeneralSocketMessage, Error>(new ConnectionError(connectionException));

                yield break;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                if (_webSocket.State != WebSocketState.Open)
                {
                    yield return Failure<GeneralSocketMessage, Error>(
                        new ConnectionLost(_webSocket.State));

                    yield break;
                }

                var idealSize = Math.Min(FrameLength, _receiveBuffer.Length - _receiveBufferWPos);
                var receiveArraySegment = new ArraySegment<byte>(_receiveBuffer, _receiveBufferWPos, idealSize);

                if (!(await TryReceiveAsync(_webSocket, receiveArraySegment, cancellationToken).ConfigureAwait(false))
                    .TryGetValue(out var received, out var receiveException))
                {
                    if (receiveException is not OperationCanceledException)
                        yield return Failure<GeneralSocketMessage, Error>(new ConnectionError(receiveException));

                    yield break;
                }

                if (received.MessageType is not WebSocketMessageType.Text)
                {
                    yield return Failure<GeneralSocketMessage, Error>(
                        new UnknownMessageType($"Received unsupported message type: {received.MessageType}"));

                    yield break;
                }

                if (!TryIncrementByChecked(ref _receiveBufferWPos, received.Count)
                    || _receiveBufferWPos > MaxMessageLength)
                {
                    yield return Failure<GeneralSocketMessage, Error>(
                        new MessageTooLong("Received message exceeds maximum length of 5 MB"));

                    yield break;
                }

                // If the message is not complete, continue receiving
                if (!received.EndOfMessage) continue;

                // Message should already be in UTF-8 format in this case, no need for encoding conversion
                var mem = _receiveBuffer.AsMemory(0, _receiveBufferWPos);

                yield return TryDeserializeMessage<GeneralSocketMessage>(mem.Span, _jsonOptions)
                    .TryGetValue(out var test, out var error)
                    ? Success<GeneralSocketMessage, Error>(test!)
                    : Failure<GeneralSocketMessage, Error>(error);

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
#if NETSTANDARD2_0
            return Failure<T?, DeserializationError>(
                new DeserializationError(ex, Encoding.UTF8.GetString(utf8Json.ToArray())));
#else
            return Failure<T?, DeserializationError>(
                new DeserializationError(ex, Encoding.UTF8.GetString(utf8Json)));
#endif
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
    }

    private bool StreamAlreadyInUse() => _webSocket is not null;

    #endregion
}