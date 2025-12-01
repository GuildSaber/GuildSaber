using GuildSaber.Common.Services.BeatLeader.Models.Responses;
using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;

namespace GuildSaber.Common.Services.BeatLeader.Models;

public static class BeatLeaderExtensions
{
    extension(LeaderboardsResponse self)
    {
        public BLLeaderboardId? FindLeaderboardId(EDifficulty difficulty, string gameMode)
            => FindLeaderboardId(self, difficulty.ToString(), gameMode);

        public BLLeaderboardId? FindLeaderboardId(string difficulty, string gameMode)
            => self.Leaderboards
                .Where(y => y.Difficulty.DifficultyName == difficulty && y.Difficulty.ModeName == gameMode)
                .Select(y => y.Id)
                .FirstOrDefault();
    }
}