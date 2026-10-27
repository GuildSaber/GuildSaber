using GuildSaber.Common.Services.ScoreSaber.Models;
using GuildSaber.Common.Services.ScoreSaber.Models.Responses;
using GuildSaber.Database.Models.Server.Scores;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties;
using GuildSaber.Database.Models.StrongTypes;

namespace GuildSaber.Database.Models.Mappers.ScoreSaber;

public static class ScoreMappers
{
    public static async ValueTask<ScoreSaberScore> MapAsync(
        this Score score, PlayerId playerId,
        SongDifficulty.SongDifficultyId songDifficultyId,
        Func<Device?, LegacyHMD?, CancellationToken, ValueTask<HmdId?>> resolveHmdId,
        Func<string?, CancellationToken, ValueTask<ControllerId?>> resolveControllerId,
        CancellationToken token) => new()
    {
        PlayerId = playerId,
        SongDifficultyId = songDifficultyId,
        ScoreSaberScoreId = score.Id,

        BaseScore = BaseScore.CreateUnsafe(score.UnmodifiedScore).Value,
        Modifiers = ModifiersMapper.ToModifiers(score.Mods),
        SetAt = score.CreatedAt,

        MaxCombo = score.MaxCombo,
        IsFullCombo = score.FullCombo,
        BadCuts = score.BadCuts,
        MissedNotes = score.MissedNotes,
        Statistics = null,

        HmdId = await resolveHmdId(score.Device, score.LegacyHmd, token),
        LeftControllerId = await resolveControllerId(score.Device?.ControllerLeft, token),
        RightControllerId = await resolveControllerId(score.Device?.ControllerRight, token)
    };
}