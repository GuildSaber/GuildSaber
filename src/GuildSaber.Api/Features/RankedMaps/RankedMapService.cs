using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.Scores;
using GuildSaber.Common.Services.BeatLeader;
using GuildSaber.Common.Services.BeatLeader.Models;
using GuildSaber.Common.Services.BeatSaver;
using GuildSaber.Common.Services.BeatSaver.Models;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;
using GuildSaber.Common.Services.ScoreSaber;
using GuildSaber.Common.Services.ScoreSaber.Models.StrongTypes;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Boosts;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.Server.RankedMaps;
using GuildSaber.Database.Models.Server.RankedMaps.MapVersions;
using GuildSaber.Database.Models.Server.RankedMaps.MapVersions.PlayModes;
using GuildSaber.Database.Models.Server.Songs;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties.GameModes;
using GuildSaber.Database.Models.StrongTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static GuildSaber.Api.Features.RankedMaps.RankedMapService.CreateResponse;
using Version = GuildSaber.Common.Services.BeatSaver.Models.Version;

namespace GuildSaber.Api.Features.RankedMaps;

public class RankedMapService(
    ServerDbContext dbContext,
    BeatSaverApi beatSaverApi,
    BeatLeaderApi beatLeaderApi,
    ScoreSaberApi scoreSaberApi,
    TimeProvider timeProvider,
    IOptions<RankedMapSettings> rankedMapSettings)
{
    /// <remarks>
    /// The multi-version feature was removed a while then, but I think the latest version in the array is the newer.
    /// </remarks>
    /// <returns>A function that takes a <see cref="BeatMap" /> and returns its latest <see cref="Version" />.</returns>
    private static Func<BeatMap, Version> SelectLatestBeatMapVersion
        => beatMap => beatMap.Versions[^1];

    public abstract record CreateResponse
    {
        public sealed record TooManyRankedMaps(int CurrentCount, int MaxCount) : CreateResponse;

        public sealed record ValidationFailure(IEnumerable<KeyValuePair<string, string[]>> Errors) : CreateResponse
        {
            public ValidationFailure(string name, string error) : this([
                new KeyValuePair<string, string[]>(name, [error])
            ]) { }

            public override string ToString()
                => string.Join("; ", Errors.SelectMany(x => x.Value.Select(err => $"{x.Key}: {err}")));
        }

        public sealed record NotOnBeatSaver(BeatSaverKey BeatSaverKey) : CreateResponse;
        public sealed record BeatSaverError(string Message) : CreateResponse;
        public sealed record RateLimited(TimeSpan RetryAfter) : CreateResponse;

        public sealed record Success(
            RankedMap RankedMap,
            Song Song,
            SongDifficulty SongDifficulty,
            GameMode GameMode
        ) : CreateResponse;

        public sealed record UnexpectedFailure(string Message) : CreateResponse;
    }

    public readonly record struct RankedMapBoostsCount(int Tier1, int Tier2, int Tier3);

    public async Task<CreateResponse> CreateRankedMap(GuildId guildId, RankedMapRequest.CreateRankedMap request)
        => await Success<int, CreateResponse>(await GetCurrentGuildRankedMapCount(guildId))
            .Check(async count => ValidateRankedMapCreationLimit(count, await GetBoostsCountsAsync(guildId))
                .MapError(CreateResponse (x) => new TooManyRankedMaps(x.current, x.max)))
            .Check(async _ => await GuildContextExistsInGuildAsync(guildId, request.ContextId) switch
            {
                true => UnitResult.Success<CreateResponse>(),
                false => Failure<CreateResponse>(new ValidationFailure("ContextId",
                    $"Guild context with ID '{request.ContextId}' does not exist in the guild."))
            })
            .Bind(async _ => await beatSaverApi
                .GetBeatMapAsync(request.BaseMapVersion.BeatSaverKey)
                .MapError(CreateResponse (err) => err switch
                {
                    BeatSaverApi.Error.RateLimitExceeded(var retryAfter) => new RateLimited(retryAfter),
                    BeatSaverApi.Error.Other(var message) => new BeatSaverError(message),
                    _ => throw new NotSupportedException($"Unhandled BeatSaver API error: {err}")
                })
                .Bind(beatmap => beatmap is null
                    ? Failure<BeatMap, CreateResponse>(new NotOnBeatSaver(request.BaseMapVersion.BeatSaverKey))
                    : Success<BeatMap, CreateResponse>(beatmap)))
            .Bind(async beatmap => (await GetGameMode(request.BaseMapVersion.Characteristic) switch
                {
                    null => Failure<GameMode, CreateResponse>(new ValidationFailure("GameMode",
                        $"Game mode '{request.BaseMapVersion.Characteristic}' is not yet supported. Please ask for it to be added.")),
                    var gameMode => Success<GameMode, CreateResponse>(gameMode)
                })
                .Map(static (gameMode, beatMap) => (beatMap, gameMode), beatmap))
            .Bind(async tuple => (await GetPlayMode(request.BaseMapVersion.PlayMode) switch
                {
                    null => Failure<PlayMode, CreateResponse>(new ValidationFailure("PlayMode",
                        $"Play mode '{request.BaseMapVersion.PlayMode}' is not yet supported. Please ask for it to be added.")),
                    var playMode => Success<PlayMode, CreateResponse>(playMode)
                })
                .Map(static (playMode, tuple) => (tuple.beatMap, tuple.gameMode, playMode), tuple))
            .Bind(async tuple => await InsertOrUpdateSongAndDifficulty(
                    tuple.beatMap,
                    SelectLatestBeatMapVersion,
                    tuple.gameMode,
                    request.BaseMapVersion.Difficulty)
                .MapError(CreateResponse (error) => new UnexpectedFailure(error))
                .Map(static (songAndDiff, tuple) =>
                    (tuple.beatMap, songAndDiff.song, songAndDiff.difficulty, tuple.playMode, tuple.gameMode), tuple))
            .Bind(tuple => CreateAndValidate(
                    guildId, tuple.beatMap, tuple.song.Id, tuple.difficulty.Id, tuple.playMode.Id, request)
                .MapError(CreateResponse (errors) => new ValidationFailure(errors))
                .Map(static (rankedMap, dbContext) => dbContext.AddAndSaveAsync(rankedMap), dbContext)
                .Map(static (rankedMap, tuple) => (rankedMap, tuple.song, tuple.difficulty, tuple.gameMode), tuple))
            .Match(tuple => new Success(tuple.rankedMap, tuple.song, tuple.difficulty, tuple.gameMode), err => err);

    private Task<bool> GuildContextExistsInGuildAsync(GuildId guildId, GuildContext.GuildContextId contextId)
        => dbContext.GuildContexts.AnyAsync(x => x.Id == contextId && x.GuildId == guildId);

    private async Task<GameMode?> GetGameMode(string name)
        => await dbContext.GameModes.FirstOrDefaultAsync(x => EF.Functions.Like(x.Name, name));

    private async Task<PlayMode?> GetPlayMode(string name)
        => await dbContext.PlayModes.FirstOrDefaultAsync(x => EF.Functions.Like(x.Name, name));

    private async Task<Result<(Song song, SongDifficulty difficulty)>> InsertOrUpdateSongAndDifficulty(
        BeatMap beatMap,
        Func<BeatMap, Version> versionSelector,
        GameMode gameMode,
        EDifficulty difficulty)
    {
        var version = versionSelector(beatMap);
        var difficultyStr = difficulty.ToString();
        var diff = version.Diffs
            .FirstOrDefault(x => x.Characteristic == gameMode.Name && x.Difficulty == difficultyStr);

        if (diff is null)
            return Failure<(Song, SongDifficulty)>(
                $"Could not find difficulty '{difficulty}' for game mode '{gameMode.Name}' in beatmap '{beatMap.Name}'.");

        var song = await dbContext.Songs
            .AsTracking()
            .Include(x => x.SongDifficulties).ThenInclude(x => x.GameMode)
            .FirstOrDefaultAsync(x => x.Hash == version.Hash);

        if (song is null)
        {
            song = new Song
            {
                Hash = version.Hash,
                BeatSaverKey = beatMap.Id,
                UploadedAt = version.CreatedAt,
                Info = new SongInfo
                {
                    BeatSaverName = beatMap.Name,
                    SongName = beatMap.Metadata.SongName,
                    SongSubName = beatMap.Metadata.SongSubName,
                    SongAuthorName = beatMap.Metadata.SongAuthorName,
                    MapperName = beatMap.Metadata.LevelAuthorName
                },
                Stats = new SongStats
                {
                    BPM = beatMap.Metadata.Bpm,
                    DurationSec = beatMap.Metadata.Duration,
                    IsAutoMapped = beatMap.Automapper
                },
                SongDifficulties = []
            };

            dbContext.Songs.Add(song);
        }

        var songDifficulty = song.SongDifficulties
            .FirstOrDefault(x => x.GameMode.Id == gameMode.Id && x.Difficulty == difficulty);
        if (songDifficulty is not null)
            return Success((song, songDifficulty));

        var (blLdTask, ssLdTask) = (
            beatLeaderApi.GetLeaderboardsAsync(version.Hash).Unwrap(),
            scoreSaberApi.GetLeaderboardInfoAsync(
                version.Hash,
                difficulty, SSGameMode.TryCreate(gameMode.Name).Unwrap()
            ).Unwrap());
        await Task.WhenAll(blLdTask, ssLdTask);

        var (ssId, blId) = (ssLdTask.Result?.Id, blLdTask.Result?.FindLeaderboardId(difficultyStr, gameMode.Name));
        if (blId is null)
            return Failure<(Song, SongDifficulty)>(
                $"Could not find BeatLeader leaderboard for song '{beatMap.Name}' " +
                $"(hash: {version.Hash}) with difficulty '{difficultyStr}' and game mode '{gameMode.Name}'.");

        songDifficulty = new SongDifficulty
        {
            BLLeaderboardId = blId,
            SSLeaderboardId = ssId,
            GameModeId = gameMode.Id,
            Difficulty = difficulty,
            Stats = new SongDifficultyStats
            (
                MaxScore: MaxScore.TryCreate(diff.MaxScore).Unwrap(),
                NoteJumpSpeed: NJS.TryCreate(diff.Njs).Unwrap(),
                NoteCount: diff.Notes,
                BombCount: diff.Bombs,
                ObstacleCount: diff.Obstacles,
                NotesPerSecond: diff.Nps,
                Duration: diff.Length
            )
        };
        song.SongDifficulties.Add(songDifficulty);
        await dbContext.SaveChangesAsync();

        return Success((song, songDifficulty));
    }

    private async Task<Result<RankedMap, List<KeyValuePair<string, string[]>>>> CreateAndValidate(
        GuildId guildId,
        BeatMap beatMap,
        Song.SongId songId,
        SongDifficultyId songDifficultyId,
        PlayMode.PlayModeId playmodeId,
        RankedMapRequest.CreateRankedMap request)
    {
        var errors = new List<KeyValuePair<string, string[]>>();

        var requirementsResult = request.Requirements.Map();
        if (!requirementsResult.TryGetValue(out var requirements, out var reqErrors))
            errors.AddRange(reqErrors);

        var accCurve = await GetFirstAccuracyCurveFromGuildAsync(guildId);
        if (!accCurve.HasValue)
            errors.Add(new KeyValuePair<string, string[]>("MissingPoint",
                ["Guild does not have any point settings configured."]));

        var categories = await dbContext.Categories
            .AsTracking()
            .Where(x => request.CategoryIds.Contains(x.Id) && x.GuildId == guildId)
            .ToListAsync();

        var categoryErrors = request.CategoryIds
            .Where(categoryId => categories.All(x => x.Id != categoryId))
            .Select(categoryId => $"Category with ID '{categoryId}' does not exist in the guild.")
            .ToArray();

        if (categoryErrors.Length > 0)
            errors.Add(new KeyValuePair<string, string[]>("CategoryIds", categoryErrors));

        if (errors.Count > 0)
            return Failure<RankedMap, List<KeyValuePair<string, string[]>>>(errors);

        var rating = null as RankedMapRating;
        if (request.ManualRating is { AccuracyStar: not null, DifficultyStar: not null })
        {
            rating = new RankedMapRating
            {
                AccStar = new RankedMapRating.AccuracyStar(request.ManualRating.AccuracyStar.Value),
                DiffStar = new RankedMapRating.DifficultyStar(request.ManualRating.DifficultyStar.Value)
            };
        }
        else
        {
            var ratingResult = await GetRankedMapRatingAsync(
                requirements!,
                SelectLatestBeatMapVersion(beatMap),
                request.BaseMapVersion.Characteristic,
                request.BaseMapVersion.Difficulty,
                accCurve.Value
            );
            if (!ratingResult.TryGetValue(out rating))
            {
                rating = new RankedMapRating
                {
                    AccStar = request.ManualRating.AccuracyStar is { } accStar
                        ? new RankedMapRating.AccuracyStar(accStar)
                        : new RankedMapRating.AccuracyStar(0),
                    DiffStar = request.ManualRating.DifficultyStar is { } diffStar
                        ? new RankedMapRating.DifficultyStar(diffStar)
                        : new RankedMapRating.DifficultyStar(0)
                };
            }
            else
            {
                rating.AccStar = request.ManualRating.AccuracyStar is { } accStar
                    ? new RankedMapRating.AccuracyStar(accStar)
                    : rating.AccStar;
                rating.DiffStar = request.ManualRating.DifficultyStar is { } diffStar
                    ? new RankedMapRating.DifficultyStar(diffStar)
                    : rating.DiffStar;
            }
        }

        return new RankedMap
        {
            GuildId = guildId,
            ContextId = request.ContextId,
            Requirements = requirements!,
            Rating = rating,
            MapVersions =
            [
                new MapVersion
                {
                    SongDifficultyId = songDifficultyId,
                    PlayModeId = playmodeId,
                    SongId = songId,
                    AddedAt = timeProvider.GetUtcNow(),
                    Order = 0
                }
            ],
            Categories = categories
        };
    }


    public async Task<Result<RankedMapRating>> GetRankedMapRatingAsync(
        RankedMapRequirements requirements,
        Version beatMapVersion,
        string characteristic,
        EDifficulty difficulty,
        CustomCurve accuracyCurve)
    {
        var exMachinaResult = await beatLeaderApi.GetExMachinaStarRatingAsync(
            beatMapVersion.Hash, difficulty, characteristic
        );
        if (!exMachinaResult.TryGetValue(out var exMachina, out var exMachinaError))
            return Failure<RankedMapRating>(exMachinaError);

        var stars = ScoringUtils.StarsFromExMachina(
            exMachina,
            requirements,
            accuracyCurve
        );

        return new RankedMapRating { AccStar = stars.Item1, DiffStar = stars.Item2 };
    }

    public Task<Maybe<CustomCurve>> GetFirstAccuracyCurveFromGuildAsync(GuildId guildId)
        => dbContext.Points.Where(x => x.GuildId == guildId)
                .Select(x => x.CurveSettings.Accuracy)
                .FirstOrDefaultAsync() switch
            {
                null => Task.FromResult(Maybe<CustomCurve>.None),
                var curve => From(curve)
            };

    public Task<int> GetCurrentGuildRankedMapCount(GuildId guildId)
        => dbContext.RankedMaps.CountAsync(x => x.GuildId == guildId);

    private UnitResult<(int current, int max)> ValidateRankedMapCreationLimit(
        int currentCount, RankedMapBoostsCount boostsCount)
    {
        var settings = rankedMapSettings.Value;
        var max = settings.DefaultSettings.MaxRankedMapCount;

        max += boostsCount.Tier1 * settings.BoostSettings.MapCountBoosts.Tier1;
        max += boostsCount.Tier2 * settings.BoostSettings.MapCountBoosts.Tier2;
        max += boostsCount.Tier3 * settings.BoostSettings.MapCountBoosts.Tier3;

        return currentCount switch
        {
            var count when count >= max => Failure((count, max)),
            _ => UnitResult.Success<(int, int)>()
        };
    }

    public async Task<RankedMapBoostsCount> GetBoostsCountsAsync(GuildId guildId) => await dbContext
            .Boosts.Where(x => x.GuildId == guildId)
            .GroupBy(x => x.Type)
            .Select(x => new { Type = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count) switch
        {
            null => new RankedMapBoostsCount(0, 0, 0),
            var boostCounts => new RankedMapBoostsCount(
                Tier1: boostCounts.GetValueOrDefault(Boost.EBoostType.Tier1, 0),
                Tier2: boostCounts.GetValueOrDefault(Boost.EBoostType.Tier2, 0),
                Tier3: boostCounts.GetValueOrDefault(Boost.EBoostType.Tier3, 0)
            )
        };
}