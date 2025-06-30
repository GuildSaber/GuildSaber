using GuildSaber.Common.Services.BeatLeader;
using GuildSaber.Common.Services.BeatLeader.Models.Responses;
using GuildSaber.Database.Contexts.Server;
using Hangfire;

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
            {
                if (!result.TryGetValue(out var value, out var error))
                    logger.LogError("WebSocket error: {Error}", error);

                _ = value switch
                {
                    SocketGeneralResponse.Upload upload => BackgroundJob.Enqueue((ScoreSyncHandler handler)
                        => handler.HandleUploadedScoreAsync(upload.Score)),
                    SocketGeneralResponse.Accepted accepted => BackgroundJob.Enqueue((ScoreSyncHandler handler)
                        => handler.HandleAcceptedScoreAsync(accepted.Score)),
                    SocketGeneralResponse.Rejected rejected => BackgroundJob.Enqueue((ScoreSyncHandler handler)
                        => handler.HandleRejectedScoreAsync(rejected.Score)),
                    _ => throw new NotImplementedException()
                };
            }

            await Task.Delay(_reconnectAfter, stoppingToken);
        } while (!stoppingToken.IsCancellationRequested);
    }
}

public sealed class ScoreSyncHandler(ServerDbContext dbContext)
{
    public Task HandleUploadedScoreAsync(UploadScoreResponse response)
        => Task.CompletedTask;

    public Task HandleAcceptedScoreAsync(AcceptedScoreResponse response)
        => Task.CompletedTask;

    public Task HandleRejectedScoreAsync(RejectedScoreResponse response)
        => Task.CompletedTask;
}