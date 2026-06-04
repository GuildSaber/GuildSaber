using System.Diagnostics;
using GuildSaber.Common.Extra;
using static GuildSaber.Api.Features.Guilds.Members.LevelStats.Http.LevelStatResponses;
using static GuildSaber.Api.Features.Guilds.Levels.Http.LevelResponses;

namespace GuildSaber.CSharpClient.Routes.Guilds.Members.LevelStats;

public static class LevelStatsUtilities
{
    extension(IEnumerable<MemberLevelStat> self)
    {
        public Level? GetGlobalLevel() => self
            .Where(x => x.Level.CategoryId is null && x is { IsLocked: false, Level: Level.RankedMapListLevel })
            .LastOrDefault(x => x.IsCompleted)
            ?.Level;

        public Level? GetCategoryLevel(CategoryId categoryId) => self
            .Where(x => x.Level.CategoryId == categoryId && x is { IsLocked: false, Level: Level.RankedMapListLevel })
            .LastOrDefault(x => x.IsCompleted)
            ?.Level;

        public TrophiesData CalculateTrophiesData()
        {
            Span<int> counts = stackalloc int[5];
            foreach (var stat in self.Where(s => s.IsCompleted))
            {
                var completionPercent = stat.Level switch
                {
                    Level.RankedMapListLevel { TotalCount: > 0 } listLevel => stat.PassCount.HasValue
                        ? stat.PassCount.Value / (float)listLevel.TotalCount
                        : 0f,
                    _ => 0f
                };

                Trace.Assert((int)Trophy.Ruby == 4, "Trophy enum values code assumption changed.");
                var trophy = Trophy.GetFromPercentage(completionPercent);
                if (trophy is null) continue;

                // Use the int value of the trophy enum to index into the counts array
                counts[(int)trophy]++;
            }

            return new TrophiesData(counts[0], counts[1], counts[2], counts[3], counts[4]);
        }

        /// <summary>
        /// Calculates a skill equilibrium score based on the standard deviation of category levels.
        /// </summary>
        /// <param name="categoryIds">The category IDs to consider for the calculation.</param>
        /// <remarks>This function will use 0 in the calculus for each missing categories.</remarks>
        /// <returns>The skill equilibrium percentage between 0 and 100f</returns>
        public double? CalculateSkillEquilibrium(IEnumerable<CategoryId> categoryIds)
        {
            var categoryLevelOrders = categoryIds
                .Select(categoryId => (int)(self.GetCategoryLevel(categoryId)?.Order ?? 0))
                .ToList();

            return categoryLevelOrders.Count > 1
                ? Math.Max(0f, 100f - StandardDeviation(categoryLevelOrders) * 100f / categoryLevelOrders.Average())
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