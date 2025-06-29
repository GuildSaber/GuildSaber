using System.Net.WebSockets;
using System.Text.Json;
using CSharpFunctionalExtensions;
using GuildSaber.Common.Services.BeatLeader.Models;
using GuildSaber.Common.Services.BeatLeader.Models.Responses;
using Error = GuildSaber.Common.Services.BeatLeader.BeatLeaderGeneralSocketStream.Error;
using static GuildSaber.Common.Services.BeatLeader.Models.Responses.SocketGeneralResponse;

namespace GuildSaber.Common.Services.BeatLeader;

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
    private bool _disposed;
    private int _receiveBufferWPos;
    private ClientWebSocket? _webSocket;

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await CleanupWebSocketAsync();
    }

    public async IAsyncEnumerator<Result<SocketGeneralResponse, Error>> GetAsyncEnumerator(
        CancellationToken cancellationToken = new())
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(BeatLeaderGeneralSocketStream));
        if (StreamAlreadyInUse())
            throw new InvalidOperationException("Stream is already in use.");

        _webSocket = new ClientWebSocket();

        var connectResult = await TryConnectWebSocket(_webSocket, _generalUri, cancellationToken);
        try
        {
            if (connectResult.TryGetError(out var connectionException))
            {
                if (connectionException is not OperationCanceledException)
                    yield return Failure<SocketGeneralResponse, Error>(new Error.ConnectionError(connectionException));

                yield break;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                if (_webSocket.State != WebSocketState.Open)
                {
                    yield return Failure<SocketGeneralResponse, Error>(
                        new Error.ConnectionLost(_webSocket.State));

                    yield break;
                }

                var idealSize = Math.Min(FrameLength, _receiveBuffer.Length - _receiveBufferWPos);
                var receiveArraySegment = new ArraySegment<byte>(_receiveBuffer, _receiveBufferWPos, idealSize);

                if (!(await TryReceiveAsync(_webSocket, receiveArraySegment, cancellationToken).ConfigureAwait(false))
                    .TryGetValue(out var received, out var receiveException))
                {
                    if (receiveException is not OperationCanceledException)
                        yield return Failure<SocketGeneralResponse, Error>(new Error.ConnectionError(receiveException));

                    yield break;
                }

                if (received.MessageType is not WebSocketMessageType.Text)
                {
                    yield return Failure<SocketGeneralResponse, Error>(
                        new Error.UnknownMessageType($"Received unsupported message type: {received.MessageType}"));

                    yield break;
                }

                if (!TryIncrementByChecked(ref _receiveBufferWPos, received.Count)
                    || _receiveBufferWPos > MaxMessageLength)
                {
                    yield return Failure<SocketGeneralResponse, Error>(
                        new Error.MessageTooLong("Received message exceeds maximum length of 5 MB"));

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
                        new Error.UnknownMessageType($"Received unknown message type '{message.Message}'"))
                };

                _receiveBufferWPos = 0;
            }
        }
        finally
        {
            await CleanupWebSocketAsync();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _webSocket?.Dispose();
        _webSocket = null;
        _receiveBufferWPos = 0;
    }

    /// <summary>
    /// Represents different types of errors that can occur during WebSocket operations.
    /// </summary>
    public abstract record Error
    {
        /// <summary>
        /// Represents errors that occur when connecting to the WebSocket.
        /// </summary>
        /// <param name="Exception">The exception that occurred during connection</param>
        public record ConnectionError(Exception Exception) : Error;

        /// <summary>
        /// Represents errors that occur when deserializing messages from the WebSocket.
        /// </summary>
        /// <param name="Message">Detailed error message</param>
        /// <param name="RawJson">The raw JSON that failed to deserialize, if available</param>
        public record DeserializationError(Exception Exception, string? RawJson = null) : Error;

        public record MessageTooLong(string Message) : Error;

        /// <summary>
        /// Represents errors for unknown or unexpected message types from the WebSocket.
        /// </summary>
        /// <param name="MessageType">The unknown message type identifier</param>
        public record UnknownMessageType(string MessageType) : Error;

        /// <summary>
        /// Represents errors that occur when the WebSocket connection is lost unexpectedly.
        /// </summary>
        /// <param name="WebSocketState">The state of the WebSocket when the error occurred</param>
        public record ConnectionLost(WebSocketState WebSocketState) : Error;
    }

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

    private static Result<T?, Error.DeserializationError> TryDeserializeMessage<T>(
        ReadOnlySpan<byte> utf8Json,
        JsonSerializerOptions? options = null)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(utf8Json, options);
        }
        catch (JsonException ex)
        {
            return Failure<T?, Error.DeserializationError>(
                new Error.DeserializationError(ex, utf8Json.ToString()));
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
}