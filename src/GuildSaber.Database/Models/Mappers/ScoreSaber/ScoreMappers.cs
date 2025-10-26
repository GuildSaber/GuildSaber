using GuildSaber.Common.Services.ScoreSaber.Models.Responses;
using GuildSaber.Database.Models.Server.Players;
using GuildSaber.Database.Models.Server.Scores;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties;
using GuildSaber.Database.Models.StrongTypes;

namespace GuildSaber.Database.Models.Mappers.ScoreSaber;

public static class ScoreMappers
{
    public static ScoreSaberScore Map(
        this Score score, Player.PlayerId playerId,
        SongDifficulty.SongDifficultyId songDifficultyId) => new()
    {
        PlayerId = playerId,
        SongDifficultyId = songDifficultyId,
        ScoreSaberScoreId = score.Id,

        BaseScore = BaseScore.CreateUnsafe(score.BaseScore).Value,
        Modifiers = ModifiersMapper.ToModifiers(score.Modifiers),
        SetAt = score.TimeSet,

        MaxCombo = score.MaxCombo,
        IsFullCombo = score.FullCombo,
        BadCuts = score.BadCuts,
        MissedNotes = score.MissedNotes,
        HMD = score.Hmd.Map(score.DeviceHmd),

        DeviceHmd = score.DeviceHmd,
        DeviceControllerLeft = score.DeviceControllerLeft,
        DeviceControllerRight = score.DeviceControllerRight
    };
}