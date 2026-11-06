using CSharpFunctionalExtensions;
using GuildSaber.Common.Services.BeatLeader;
using GuildSaber.Common.Services.BeatLeader.Models;
using GuildSaber.Common.Services.BeatSaver.Models;
using GuildSaber.Common.Services.ScoreSaber;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Songs;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties.GameModes;
using Microsoft.EntityFrameworkCore;
using Version = GuildSaber.Common.Services.BeatSaver.Models.Version;

namespace GuildSaber.Api.Features.Songs;

public sealed class SongService(ServerDbContext dbContext, BeatLeaderApi beatLeaderApi, ScoreSaberApi scoreSaberApi)
{
    internal sealed record ResolvedDifficulty(
        BeatMap BeatMap,
        Version Version,
        GameMode GameMode,
        EDifficulty Difficulty,
        SongDifficultyStats Stats,
        BLLeaderboardId? BeatLeaderId,
        SSLeaderboardId? ScoreSaberId);

    /// <summary>Resolves a difficulty and its leaderboard IDs, staging any changes for the caller to save.</summary>
    /// <remarks>Requires a BeatLeader leaderboard. A missing ScoreSaber ID can be resolved from a later score.</remarks>
    public async Task<Result<(Song song, SongDifficulty difficulty)>> GetOrCreateDifficultyAsync(
        BeatMap beatMap, Func<BeatMap, Version?> versionSelector, GameMode gameMode, EDifficulty difficulty,
        CancellationToken token)
        => await ResolveDifficultyAsync(beatMap, versionSelector, gameMode, difficulty, token)
            .Map(static (resolved, state) => state.service.StageDifficultyAsync(resolved, state.token),
                (service: this, token));

    /// <summary>Fetches remote metadata without staging changes or holding a database transaction.</summary>
    internal async Task<Result<ResolvedDifficulty>> ResolveDifficultyAsync(
        BeatMap beatMap, Func<BeatMap, Version?> versionSelector, GameMode gameMode, EDifficulty difficulty,
        CancellationToken token)
    {
        if (beatMap.Versions.Count == 0 || versionSelector(beatMap) is not { } version)
            return Failure<ResolvedDifficulty>($"The requested version is not available for map '{beatMap.Id}'.");

        var diff = version.Diffs.FirstOrDefault(x =>
            x.Characteristic == gameMode.Name && x.Difficulty == difficulty.ToString());

        if (diff is null)
            return Failure<ResolvedDifficulty>(
                $"Could not find difficulty '{difficulty}' for characteristic '{gameMode.Name}' in version '{version.Hash}'.");

        if (!MaxScore.TryCreate(diff.MaxScore).TryGetValue(out var maxScore, out var scoreError))
            return Failure<ResolvedDifficulty>(scoreError);

        var stats = new SongDifficultyStats(maxScore, diff.Njs, diff.Notes, diff.Bombs, diff.Obstacles,
            diff.Nps, diff.Length);
        var songDifficulty = await dbContext.SongDifficulties
            .Where(x =>
                x.Song.Hash == version.Hash
                && x.GameModeId == gameMode.Id
                && x.Difficulty == difficulty)
            .Select(x => new { x.BLLeaderboardId, x.SSLeaderboardId })
            .SingleOrDefaultAsync(token);

        if (songDifficulty is { BLLeaderboardId: not null, SSLeaderboardId: not null })
            return new ResolvedDifficulty(beatMap, version, gameMode, difficulty, stats,
                songDifficulty.BLLeaderboardId, songDifficulty.SSLeaderboardId);

        if (!SSGameMode.TryCreate(gameMode.Name).TryGetValue(out var ssGameMode, out var gameModeError))
            return Failure<ResolvedDifficulty>(gameModeError);

        var blTask = beatLeaderApi.GetLeaderboardsAsync(version.Hash, token);
        var ssTask = scoreSaberApi.GetLeaderboardInfoAsync(version.Hash, difficulty, ssGameMode, token);
        await Task.WhenAll(blTask, ssTask);

        var blResult = await blTask;
        var ssResult = await ssTask;

        if (blResult.IsFailure)
            return Failure<ResolvedDifficulty>(blResult.Error);
        if (ssResult.IsFailure)
            return Failure<ResolvedDifficulty>(ssResult.Error);

        var blId = blResult.Value?.FindLeaderboardId(difficulty, gameMode.Name);
        var ssId = ssResult.Value?.Id;
        if (songDifficulty?.BLLeaderboardId is null && blId is null)
            return Failure<ResolvedDifficulty>(
                $"No BeatLeader leaderboard exists for version '{version.Hash}', characteristic '{gameMode.Name}', difficulty '{difficulty}'.");

        return new ResolvedDifficulty(beatMap, version, gameMode, difficulty, stats,
            songDifficulty?.BLLeaderboardId ?? blId, songDifficulty?.SSLeaderboardId ?? ssId);
    }

    /// <summary>Rechecks current identities inside the caller's short persistence transaction.</summary>
    internal async Task<(Song song, SongDifficulty difficulty)> StageDifficultyAsync(
        ResolvedDifficulty resolved, CancellationToken token)
    {
        var (beatMap, version, gameMode, difficulty, stats, blId, ssId) = resolved;
        var song = dbContext.Songs.Local.FirstOrDefault(x => x.Hash == version.Hash)
                   ?? await dbContext.Songs.AsTracking()
                       .Include(x => x.SongDifficulties)
                       .SingleOrDefaultAsync(x => x.Hash == version.Hash, token);

        if (song is not null && dbContext.Entry(song).State != EntityState.Added)
            await dbContext.Entry(song).Collection(x => x.SongDifficulties).LoadAsync(token);

        var songDifficulty = song?.SongDifficulties
            .FirstOrDefault(x => x.GameModeId == gameMode.Id && x.Difficulty == difficulty);

        if (song is null)
        {
            song = new Song
            {
                Hash = version.Hash, BeatSaverKey = beatMap.Id, UploadedAt = version.CreatedAt,
                Info = new SongInfo(beatMap.Name, beatMap.Metadata.SongName, beatMap.Metadata.SongSubName,
                    beatMap.Metadata.SongAuthorName, beatMap.Metadata.LevelAuthorName),
                Stats = new SongStats(beatMap.Metadata.Bpm, beatMap.Metadata.Duration, beatMap.Automapper),
                SongDifficulties = []
            };
            dbContext.Songs.Add(song);
        }

        if (songDifficulty is null)
        {
            songDifficulty = new SongDifficulty
            {
                BLLeaderboardId = blId, SSLeaderboardId = ssId, GameModeId = gameMode.Id, Difficulty = difficulty,
                Stats = stats
            };
            song.SongDifficulties.Add(songDifficulty);
        }
        else
        {
            // A cached difficulty may predate its leaderboards. Never clear identities already resolved.
            if (songDifficulty.BLLeaderboardId is null)
                dbContext.Entry(songDifficulty).Property(x => x.BLLeaderboardId).CurrentValue = blId;
            if (songDifficulty.SSLeaderboardId is null)
                dbContext.Entry(songDifficulty).Property(x => x.SSLeaderboardId).CurrentValue = ssId;
        }

        return (song, songDifficulty);
    }
}