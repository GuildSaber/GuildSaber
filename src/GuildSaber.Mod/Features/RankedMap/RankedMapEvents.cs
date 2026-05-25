using GuildSaber.Api.Features.RankedMaps;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;

namespace GuildSaber.Mod.Features.RankedMap;

public sealed record RankedMapEventData(
    BeatmapKey BeatmapKey,
    SongHash? SongHash,
    RankedMapResponses.RankedMapWithScores? RankedMapWithScores
);