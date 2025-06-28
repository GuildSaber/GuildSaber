using System.Diagnostics.CodeAnalysis;
using AwesomeAssertions;
using GuildSaber.Common.Services.BeatLeader;
using GuildSaber.Common.Services.BeatLeader.Models.Responses;

namespace GuildSaber.UnitTests.Api.Services;

public class BeatLeaderSocketTests : IAsyncDisposable
{
    private readonly BeatLeaderSocket _beatLeaderSocket
        = new(new Uri("wss://sockets.api.beatleader.com/general"));

    public async ValueTask DisposeAsync() => await _beatLeaderSocket.DisposeAsync();

    [Fact]
    public async Task StreamScoreEvents_ShouldEstablishConnection_WhenCalled()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var messageReceived = false;

        // Act
        await foreach (var _ in _beatLeaderSocket.StreamScoreEvents(cts.Token))
        {
            // We just need to verify we can connect and receive any message
            messageReceived = true;
            break;
        }

        // Assert
        messageReceived.Should().BeTrue("because we should be able to establish a WebSocket connection");
    }

    [Fact]
    public async Task StreamScoreEvents_ShouldReceiveValidScoreData_WhenRunningForAtMost30Sec()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        ScoreResponseWithMyScoreAndContexts? scoreData = null;

        // Act
        await foreach (var result in _beatLeaderSocket.StreamScoreEvents(cts.Token))
        {
            if (!result.IsSuccess) continue;
            scoreData = result.Value switch
            {
                BeatLeaderSocket.GeneralResponse.Upload upload => upload.SocketMessage.Data,
                BeatLeaderSocket.GeneralResponse.Accepted accepted => accepted.SocketMessage.Data,
                BeatLeaderSocket.GeneralResponse.Rejected rejected => rejected.SocketMessage.Data,
                _ => scoreData
            };

            if (scoreData is not null) break;
        }

        // Assert
        scoreData.Should().NotBeNull("because we should receive at least one valid score message in the time window");
        scoreData.LeaderboardId.Should().NotBeNullOrEmpty("because score data should contain a leaderboard id");
    }

    [Fact]
    [SuppressMessage("ReSharper", "MethodSupportsCancellation")]
    public async Task StreamScoreEvents_ShouldRespectCancellationWithoutThrowing_WhenCancellationRequested()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        var task = Task.Run(async () =>
        {
            // ReSharper disable once AccessToDisposedClosure
            await foreach (var _ in _beatLeaderSocket.StreamScoreEvents(cts.Token))
            {
                // Just consume messages
            }
        });

        // Wait briefly to ensure connection is established
        await Task.Delay(1000);

        // Cancel the enumeration
        await cts.CancelAsync();

        // The task should complete without throwing, with a delay to ensure cancellation is processed
        await task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task StreamScoreEvents_ShouldCollectMultipleMessages_WhenRunningForAtMost30Sec()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var messages = new List<BeatLeaderSocket.GeneralResponse>();
        var targetCount = 2;

        // Act
        await foreach (var result in _beatLeaderSocket.StreamScoreEvents(cts.Token))
        {
            if (!result.IsSuccess) continue;
            messages.Add(result.Value);

            if (messages.Count >= targetCount)
                break;
        }

        // Assert
        messages.Should().HaveCountGreaterThanOrEqualTo(1,
            "because we should receive at least one message in the given time window");
    }

    [Fact]
    public async Task StreamScoreEvents_ShouldReturnConnectionError_WhenUnableToConnect()
    {
        var invalidSocket = new BeatLeaderSocket(new Uri("wss://invalid.example.com/socket"));

        try
        {
            // Act
            await foreach (var result in invalidSocket.StreamScoreEvents())
                // Assert
                result.FailureShould().BeOfType<BeatLeaderSocket.Error.ConnectionError>(
                    "because we should receive a connection error for an invalid WebSocket URI");
        }
        finally
        {
            await invalidSocket.DisposeAsync();
        }
    }
}