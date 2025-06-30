using GuildSaber.Common.Services.BeatLeader;

namespace GuildSaber.Api.Features.Scores;

public class ScoreSyncService(
    BeatLeaderGeneralSocketStream beatLeaderGeneralSocketStream,
    ILogger<ScoreSyncService> logger) : BackgroundService
{
    private readonly TimeSpan _reconnectAfter = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        do
        {
            await foreach (var result in beatLeaderGeneralSocketStream.WithCancellation(stoppingToken))
                if (result.TryGetError(out var error))
                    logger.LogError("WebSocket error: {Error}", error);

            await Task.Delay(_reconnectAfter, stoppingToken);
        } while (!stoppingToken.IsCancellationRequested);
    }
}