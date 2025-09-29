using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using AwesomeAssertions;
using GuildSaber.Common.Services.BeatLeader;
using GuildSaber.Common.Services.BeatLeader.Errors;
using GuildSaber.Common.Services.BeatLeader.Models.Responses;

namespace GuildSaber.Common.UnitTests.Services.BeatLeader;

[SuppressMessage("ReSharper", "AccessToDisposedClosure")]
[SuppressMessage("ReSharper", "MethodSupportsCancellation")]
public class BeatLeaderSocketTests : IAsyncDisposable
{
    private readonly BeatLeaderGeneralSocketStream _stream
        = new(new Uri("wss://sockets.api.beatleader.com/"));

    public async ValueTask DisposeAsync() => await _stream.DisposeAsync();

    [Test]
    public async Task GetAsyncEnumerator_ShouldEstablishWebSocketConnection_WhenEnumerationStarts()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var connectionEstablished = false;

        // Act
        await foreach (var _ in _stream.WithCancellation(cts.Token))
        {
            connectionEstablished = _stream.State == WebSocketState.Open;
            break;
        }

        // Assert
        connectionEstablished.Should()
            .BeTrue("because the stream should establish a WebSocket connection when enumeration begins");
    }

    [Test]
    public async Task GetAsyncEnumerator_ShouldReceiveValidScoreMessages_WhenConnected()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // Act
        await foreach (var result in _stream.WithCancellation(cts.Token))
        {
            if (!result.TryGetValue(out var value)) continue;

            if (value is not GeneralSocketMessage<UploadedScore> &&
                value is not GeneralSocketMessage<AcceptedScore> &&
                value is not GeneralSocketMessage<RejectedScore>)
            {
                result.FailureShould().BeOfType<ClientWebSocketStreamError.UnknownMessageType>(
                    "because we expect only known message types from the stream");
                continue;
            }

            break;
        }
    }

    [Test]
    public async Task GetAsyncEnumerator_ShouldThrowInvalidOperationException_WhenStreamAlreadyInUse()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var firstEnumerationTask = Task.Run(async () =>
        {
            await foreach (var _ in _stream.WithCancellation(cts.Token))
                // Keep the first enumeration running
                await Task.Delay(100, cts.Token);
        });

        // Wait a bit to ensure first enumeration starts
        await Task.Delay(500);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in _stream) break;
        });

        exception!.Message.Should().Be("Stream is already in use.");

        // Cleanup
        await cts.CancelAsync();
        await firstEnumerationTask;
    }

    [Test]
    public async Task GetAsyncEnumerator_ShouldAllowReuse_AfterPreviousEnumerationCompletes()
    {
        // Arrange & Act - First enumeration
        using var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var firstMessageReceived = false;

        await foreach (var _ in _stream.WithCancellation(cts1.Token))
        {
            firstMessageReceived = true;
            break;
        }

        // Act - Second enumeration after first completes
        using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var secondMessageReceived = false;

        await foreach (var _ in _stream.WithCancellation(cts2.Token))
        {
            secondMessageReceived = true;
            break;
        }

        await Task.Delay(5000); // Ensure second enumeration completes

        // Assert
        firstMessageReceived.Should().BeTrue("because the first enumeration should receive messages");
        secondMessageReceived.Should().BeTrue("because the stream should be reusable after completion");
    }

    [Test]
    public async Task GetAsyncEnumerator_ShouldHandleCancellationGracefully_WhenCancellationRequested()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        var enumerationTask = Task.Run(async () =>
        {
            await foreach (var _ in _stream.WithCancellation(cts.Token))
            {
                // Consume messages until cancelled
            }
        });

        // Wait briefly to ensure connection is established
        await Task.Delay(1000);
        await cts.CancelAsync();

        // Assert - Task should complete without throwing
        await enumerationTask.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task GetAsyncEnumerator_ShouldReceiveMultipleMessageTypes_WhenConnected()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var messages = new List<GeneralSocketMessage>();
        const int targetCount = 3;

        // Act
        await foreach (var result in _stream.WithCancellation(cts.Token))
        {
            if (!result.IsSuccess) continue;

            messages.Add(result.Value);
            if (messages.Count >= targetCount) break;
        }

        // Assert
        messages.Should().HaveCountGreaterThanOrEqualTo(1, "because at least one message should be received");
        messages.Should().OnlyContain(m =>
                m.GetType() == typeof(GeneralSocketMessage<UploadedScore>)
                || m.GetType() == typeof(GeneralSocketMessage<AcceptedScore>)
                || m.GetType() == typeof(GeneralSocketMessage<RejectedScore>),
            "because all messages should be of known types");
    }

    [Test]
    public async Task GetAsyncEnumerator_ShouldReturnConnectionError_WhenUnableToConnect()
    {
        // Arrange
        await using var invalidStream =
            new BeatLeaderGeneralSocketStream(new Uri("wss://invalid.nonexistent.domain/socket"));

        // Act & Assert
        await foreach (var result in invalidStream)
        {
            result.FailureShould().BeOfType<ClientWebSocketStreamError.ConnectionError>(
                "because connection to an invalid URI should result in a connection error");
            break;
        }
    }

    [Test]
    [SuppressMessage("ReSharper", "MethodHasAsyncOverload")]
    public async Task Dispose_ShouldCleanupResources_WhenCalled()
    {
        // Arrange
        var stream = new BeatLeaderGeneralSocketStream(new Uri("wss://sockets.api.beatleader.com/"));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Start enumeration
        var enumerationTask = Task.Run(async () =>
        {
            await foreach (var _ in stream.WithCancellation(cts.Token)) break; // Get one message then stop
        });

        await enumerationTask;

        // Act
        stream.Dispose();

        // Assert - Should be able to use the stream again after dispose
        var canReuseAfterDispose = true;
        try
        {
            using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await foreach (var _ in stream.WithCancellation(cts2.Token)) break;
        }
        catch
        {
            canReuseAfterDispose = false;
        }

        canReuseAfterDispose.Should().BeTrue("because the stream should be reusable after dispose");
    }

    [Test]
    public async Task DisposeAsync_ShouldCleanupResourcesGracefully_WhenCalled()
    {
        // Arrange
        var stream = new BeatLeaderGeneralSocketStream(new Uri("wss://sockets.api.beatleader.com/general"));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Start enumeration
        var enumerationTask = Task.Run(async () =>
        {
            await foreach (var _ in stream.WithCancellation(cts.Token)) break;
        });

        await enumerationTask;

        // Act
        await stream.DisposeAsync();

        // Assert - Should be able to use the stream again
        var canReuseAfterAsyncDispose = true;
        try
        {
            using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await foreach (var _ in stream.WithCancellation(cts2.Token)) break;
        }
        catch
        {
            canReuseAfterAsyncDispose = false;
        }

        canReuseAfterAsyncDispose.Should().BeTrue("because the stream should be reusable after async dispose");
    }
}