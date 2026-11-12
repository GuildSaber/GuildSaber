using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.RankedMaps.Http;
using GuildSaber.Common.Services.BeatSaver;
using GuildSaber.Database.Contexts.Server;
using Microsoft.EntityFrameworkCore;
using static GuildSaber.Api.Features.MapRequests.Http.MapProposalRequests;
using static GuildSaber.Api.Features.MapRequests.Http.MapRequestResponses;

namespace GuildSaber.Api.Features.MapRequests.Services;

public sealed class MapRequestLookupService(ServerDbContext dbContext, BeatSaverApi beatSaverApi)
{
    public async Task<Result<SongLookup?, BeatSaverApi.Error>> LookupAsync(SongReference song, CancellationToken token)
    {
        if (!(await (song switch
            {
                SongReference.ByKey(var key) => beatSaverApi.GetBeatMapAsync(key, token),
                SongReference.ByHash(var hash) => beatSaverApi.GetBeatMapAsync(hash, token)
            })).TryGetValue(out var beatMap, out var error))
            return Failure<SongLookup?, BeatSaverApi.Error>(error);

        if (beatMap is null || beatMap.Versions.Count == 0)
            return Success<SongLookup?, BeatSaverApi.Error>(null);

        var version = song switch
        {
            SongReference.ByKey => BeatSaverApi.SelectLatestBeatMapVersion(beatMap),
            SongReference.ByHash(var hash) => BeatSaverApi.SelectBeatMapVersionByHash(hash)(beatMap)
        };
        if (version is null) return Success<SongLookup?, BeatSaverApi.Error>(null);

        var characteristics = await dbContext.GameModes.Select(x => x.Name).ToArrayAsync(token);
        var playModes = await dbContext.PlayModes.OrderBy(x => x.Name).Select(x => x.Name).ToArrayAsync(token);

        return new SongLookup(new SongSource(beatMap.Id, version.Hash,
                new RankedMapResponses.SongInfo(beatMap.Name, beatMap.Metadata.SongName, beatMap.Metadata.SongSubName,
                    beatMap.Metadata.SongAuthorName, beatMap.Metadata.LevelAuthorName),
                new RankedMapResponses.SongStats(beatMap.Metadata.Bpm, beatMap.Metadata.Duration, beatMap.Automapper)),
            version.Diffs.Where(x => x.MaxScore > 0 && characteristics.Contains(x.Characteristic))
                .Select(x => Enum.TryParse<EDifficulty>(x.Difficulty, true, out var difficulty)
                             && Enum.IsDefined(difficulty)
                    ? new DifficultyOption(difficulty, x.Characteristic,
                        new RankedMapResponses.SongDifficultyStats(x.MaxScore, x.Njs, x.Notes, x.Bombs, x.Obstacles,
                            x.Nps, x.Length))
                    : null)
                .OfType<DifficultyOption>()
                .OrderBy(x => x.Characteristic).ThenByDescending(x => x.Difficulty).ToArray(),
            playModes);
    }
}