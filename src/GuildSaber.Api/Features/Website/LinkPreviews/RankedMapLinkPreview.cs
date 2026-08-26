using GuildSaber.Database.Models.Server.RankedMaps;
using GuildSaber.Database.Models.Server.Songs;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties;
using GuildSaber.Database.Models.StrongTypes;

namespace GuildSaber.Api.Features.Website.LinkPreviews;

internal sealed record RankedMapLinkPreview(
    RankedMapId Id,
    SongHash Hash,
    BeatSaverKey? BeatSaverKey,
    SongInfo SongInfo,
    SongStats SongStats,
    EDifficulty Difficulty,
    string GameMode,
    SongDifficultyStats DifficultyStats,
    RankedMapRating Rating,
    Name_2_50[] Categories,
    RankedMapRequirements Requirements
);