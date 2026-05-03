using System;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;

namespace GuildSaber.Mod.Core.UI.Extensions;

public static class BeatMapDifficultyExtension
{
    extension(BeatmapDifficulty self)
    {
        public EDifficulty ToEDifficulty() => self switch
        {
            BeatmapDifficulty.Easy => EDifficulty.Easy,
            BeatmapDifficulty.Normal => EDifficulty.Normal,
            BeatmapDifficulty.Hard => EDifficulty.Hard,
            BeatmapDifficulty.Expert => EDifficulty.Expert,
            BeatmapDifficulty.ExpertPlus => EDifficulty.ExpertPlus,
            _ => throw new ArgumentOutOfRangeException(nameof(self), self, null)
        };
    }
}