using System.Net.WebSockets;

// ReSharper disable once CheckNamespace
namespace GuildSaber.Common.Services.BeatLeader.Errors;

/// <summary>
/// Represents different types of errors that can occur during WebSocket operations.
/// </summary>
public abstract record ClientWebSocketStreamError
{
    /// <summary>
    /// Represents errors that occur when connecting to or communicating with the WebSocket.
    /// </summary>
    /// <param name="Exception">The underlying exception that caused the connection error.</param>
    public sealed record ConnectionError(Exception Exception) : ClientWebSocketStreamError;

    /// <summary>
    /// Represents errors that occur when deserializing JSON messages from the WebSocket.
    /// </summary>
    /// <param name="Exception">The JSON deserialization exception.</param>
    /// <param name="RawJson">The raw JSON string that failed to deserialize, if available.</param>
    public sealed record DeserializationError(Exception Exception, string? RawJson = null) : ClientWebSocketStreamError;

    /// <summary>
    /// Represents errors when a received message exceeds the maximum allowed length of 5MB.
    /// </summary>
    /// <param name="Message">A descriptive error message.</param>
    public sealed record MessageTooLong(string Message) : ClientWebSocketStreamError;

    /// <summary>
    /// Represents errors for unknown or unsupported message types received from the WebSocket.
    /// </summary>
    /// <param name="MessageType">The unknown message type identifier or WebSocket message type.</param>
    public sealed record UnknownMessageType(string MessageType) : ClientWebSocketStreamError;

    /// <summary>
    /// Represents errors that occur when the WebSocket connection is lost unexpectedly.
    /// </summary>
    /// <param name="WebSocketState">The state of the WebSocket when the connection was lost.</param>
    public sealed record ConnectionLost(WebSocketState WebSocketState) : ClientWebSocketStreamError;
}