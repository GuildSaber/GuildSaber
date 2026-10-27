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
    /// <summary>Resolves a difficulty and its leaderboard IDs, staging any changes for the caller to save.</summary>
    /// <remarks>Requires a BeatLeader leaderboard. A missing ScoreSaber ID can be resolved from a later score.</remarks>
    public async Task<Result<(Song song, SongDifficulty difficulty)>> GetOrCreateDifficultyAsync(
        BeatMap beatMap, Func<BeatMap, Version?> versionSelector, GameMode gameMode, EDifficulty difficulty,
        CancellationToken token)
    {
        if (beatMap.Versions.Count == 0 || versionSelector(beatMap) is not { } version)
            return Failure<(Song, SongDifficulty)>($"The requested version is not available for map '{beatMap.Id}'.");

        var diff = version.Diffs.FirstOrDefault(x => x.Characteristic == gameMode.Name
                                                     && x.Difficulty == difficulty.ToString());

        if (diff is null)
            return Failure<(Song, SongDifficulty)>(
                $"Could not find difficulty '{difficulty}' for characteristic '{gameMode.Name}' in version '{version.Hash}'.");

        if (!MaxScore.TryCreate(diff.MaxScore).TryGetValue(out var maxScore, out var scoreError))
            return Failure<(Song, SongDifficulty)>(scoreError);

        var song = dbContext.Songs.Local.FirstOrDefault(x => x.Hash == version.Hash)
                   ?? await dbContext.Songs.AsTracking().Include(x => x.SongDifficulties)
                       .SingleOrDefaultAsync(x => x.Hash == version.Hash, token);

        if (song is not null && dbContext.Entry(song).State != EntityState.Added)
            await dbContext.Entry(song).Collection(x => x.SongDifficulties).LoadAsync(token);

        var songDifficulty = song?.SongDifficulties
            .FirstOrDefault(x => x.GameModeId == gameMode.Id && x.Difficulty == difficulty);
        if (songDifficulty is { BLLeaderboardId: not null, SSLeaderboardId: not null })
            return (song!, songDifficulty);

        if (!SSGameMode.TryCreate(gameMode.Name).TryGetValue(out var ssGameMode, out var gameModeError))
            return Failure<(Song, SongDifficulty)>(gameModeError);

        var blTask = beatLeaderApi.GetLeaderboardsAsync(version.Hash, token);
        var ssTask = scoreSaberApi.GetLeaderboardInfoAsync(version.Hash, difficulty, ssGameMode, token);
        await Task.WhenAll(blTask, ssTask);

        if (!(await blTask).TryGetValue(out var blLeaderboards, out var blError))
            return Failure<(Song, SongDifficulty)>(blError);
        if (!(await ssTask).TryGetValue(out var ssLeaderboard, out var ssError))
            return Failure<(Song, SongDifficulty)>(ssError);

        var blId = blLeaderboards?.FindLeaderboardId(difficulty, gameMode.Name);
        var ssId = ssLeaderboard?.Id;
        if (songDifficulty?.BLLeaderboardId is null && blId is null)
            return Failure<(Song, SongDifficulty)>(
                $"No BeatLeader leaderboard exists for version '{version.Hash}', characteristic '{gameMode.Name}', difficulty '{difficulty}'.");

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
                Stats = new SongDifficultyStats(maxScore, diff.Njs, diff.Notes, diff.Bombs, diff.Obstacles,
                    diff.Nps,
                    diff.Length)
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