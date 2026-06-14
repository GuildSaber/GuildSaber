using GuildSaber.Api.Features.RankedMaps.Http;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;

namespace GuildSaber.Mod.Features.RankedMap;

public readonly record struct RankedMapEventData(
    BeatmapKey BeatmapKey,
    SongHash? SongHash,
    RankedMapResponses.RankedMapWithScores? RankedMapWithScores
);