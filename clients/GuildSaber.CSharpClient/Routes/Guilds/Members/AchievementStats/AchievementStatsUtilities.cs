using System.Diagnostics;
using GuildSaber.Common.Extra;
using static GuildSaber.Api.Features.Guilds.Members.AchievementStats.Http.AchievementStatResponses;
using static GuildSaber.Api.Features.Guilds.Achievements.Http.AchievementResponses;

namespace GuildSaber.CSharpClient.Routes.Guilds.Members.AchievementStats;

public static class AchievementStatsUtilities
{
    extension(IEnumerable<MemberAchievementStat> self)
    {
        public Achievement? GetGlobalAchievement() => self
            .Where(x => x.Achievement.CategoryId is null &&
                        x is { IsLocked: false, Achievement.Progression: AchievementProgression.Ordered })
            .LastOrDefault(x => x.IsCompleted)
            ?.Achievement;

        public Achievement? GetCategoryAchievement(CategoryId categoryId) => self
            .Where(x => x.Achievement.CategoryId == categoryId &&
                        x is { IsLocked: false, Achievement.Progression: AchievementProgression.Ordered })
            .LastOrDefault(x => x.IsCompleted)
            ?.Achievement;

        public TrophiesData CalculateTrophiesData()
        {
            Span<int> counts = stackalloc int[5];
            foreach (var stat in self.Where(s => s.IsCompleted))
            {
                var totalCount = stat.Achievement switch
                {
                    Achievement.RankedMapListAchievement achievement => achievement.TotalCount,
                    Achievement.DiffStarAchievement achievement => achievement.TotalCount,
                    Achievement.AccStarAchievement achievement => achievement.TotalCount,
                    _ => 0
                };
                var completionPercent = totalCount > 0 && stat.PassCount.HasValue
                    ? stat.PassCount.Value / (float)totalCount
                    : 0f;

                Trace.Assert((int)Trophy.Ruby == 4, "Trophy enum values code assumption changed.");
                var trophy = Trophy.GetFromPercentage(completionPercent);
                if (trophy is null) continue;

                // Use the int value of the trophy enum to index into the counts array
                counts[(int)trophy]++;
            }

            return new TrophiesData(counts[0], counts[1], counts[2], counts[3], counts[4]);
        }

        /// <summary>
        /// Calculates a skill equilibrium score based on the standard deviation of category achievements.
        /// </summary>
        /// <param name="categoryIds">The category IDs to consider for the calculation.</param>
        /// <remarks>This function will use 0 in the calculus for each missing categories.</remarks>
        /// <returns>The skill equilibrium percentage between 0 and 100f</returns>
        public double? CalculateSkillEquilibrium(IEnumerable<CategoryId> categoryIds)
        {
            var categoryAchievementOrders = categoryIds
                .Select(categoryId => self.GetCategoryAchievement(categoryId)?.Progression is
                    AchievementProgression.Ordered progression
                    ? (int)progression.Order
                    : 0)
                .ToList();
            var average = categoryAchievementOrders.Count == 0 ? 0 : categoryAchievementOrders.Average();

            return categoryAchievementOrders.Count > 1 && average > 0
                ? Math.Max(0f, 100f - StandardDeviation(categoryAchievementOrders) * 100f / average)
                : 100f;
        }
    }

    private static double StandardDeviation(IReadOnlyCollection<int> sequence)
    {
        if (sequence.Count == 0)
            return 0;

        var average = sequence.Average();
        var sum = sequence.Sum(x => Math.Pow(x - average, 2));

        return Math.Sqrt(sum / (sequence.Count - 1));
    }
}