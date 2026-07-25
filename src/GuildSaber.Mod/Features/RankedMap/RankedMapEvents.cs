using GuildSaber.Api.Features.RankedMaps.Http;
using GuildSaber.Mod.Features.GuildSaber.Runtime;

namespace GuildSaber.Mod.Features.RankedMap;

public readonly record struct RankedMapEventData(
    BeatmapKey BeatmapKey,
    SongHash? SongHash,
    RankedMapResponses.RankedMapWithScores? RankedMapWithScores,
    GuildSaberSnapshot? Snapshot
);