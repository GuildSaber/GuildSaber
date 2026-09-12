using System.Linq.Expressions;
using GuildSaber.Database.Models.Server.Guilds.Achievements;
using GuildSaber.Database.Models.Server.Guilds.Achievements.Types;

namespace GuildSaber.Api.Features.Guilds.Achievements.Http;

public class AchievementMappers
{
    public static Expression<Func<Achievement, AchievementResponses.Achievement>> MapAchievementExpression
        => achievement => achievement is RankedMapListAchievement
            ? new AchievementResponses.Achievement.RankedMapListAchievement(
                achievement.Id.Value,
                achievement.GuildId,
                achievement.ContextId,
                achievement.CategoryId,
                new AchievementResponses.AchievementInfo(achievement.Info.Name, achievement.Info.Color.ToArgb()),
                new AchievementResponses.AchievementDiscordBindings(achievement.DiscordBindings.RoleId),
                achievement.ProgressionOrder == null
                    ? new AchievementResponses.AchievementProgression.Unordered(achievement.Id.Value)
                    : new AchievementResponses.AchievementProgression.Ordered(
                        achievement.ProgressionOrder.Value,
                        achievement.IsLocking),
                achievement.UnlockXp,
                ((RankedMapListAchievement)achievement).RequiredPassCount,
                ((RankedMapListAchievement)achievement).RankedMaps.Count)
            : achievement is DiffStarAchievement
                ? new AchievementResponses.Achievement.DiffStarAchievement(
                    achievement.Id.Value,
                    achievement.GuildId,
                    achievement.ContextId,
                    achievement.CategoryId,
                    new AchievementResponses.AchievementInfo(achievement.Info.Name,
                        achievement.Info.Color.ToArgb()),
                    new AchievementResponses.AchievementDiscordBindings(achievement.DiscordBindings.RoleId),
                    achievement.ProgressionOrder == null
                        ? new AchievementResponses.AchievementProgression.Unordered(achievement.Id.Value)
                        : new AchievementResponses.AchievementProgression.Ordered(
                            achievement.ProgressionOrder.Value,
                            achievement.IsLocking),
                    achievement.UnlockXp,
                    ((DiffStarAchievement)achievement).MinStar,
                    ((DiffStarAchievement)achievement).RequiredPassCount,
                    achievement.Context.RankedMaps.Count(map =>
                        (achievement.CategoryId == null ||
                         map.Categories.Any(category => category.Id == achievement.CategoryId.Value)) &&
                        map.Rating.DiffStar >= ((DiffStarAchievement)achievement).MinStar &&
                        (((DiffStarAchievement)achievement).MaxStar == null ||
                         map.Rating.DiffStar < ((DiffStarAchievement)achievement).MaxStar!.Value)),
                    ((DiffStarAchievement)achievement).MaxStar == null
                        ? null
                        : ((DiffStarAchievement)achievement).MaxStar!.Value.Value)
                : new AchievementResponses.Achievement.AccStarAchievement(
                    achievement.Id.Value,
                    achievement.GuildId,
                    achievement.ContextId,
                    achievement.CategoryId,
                    new AchievementResponses.AchievementInfo(achievement.Info.Name,
                        achievement.Info.Color.ToArgb()),
                    new AchievementResponses.AchievementDiscordBindings(achievement.DiscordBindings.RoleId),
                    achievement.ProgressionOrder == null
                        ? new AchievementResponses.AchievementProgression.Unordered(achievement.Id.Value)
                        : new AchievementResponses.AchievementProgression.Ordered(
                            achievement.ProgressionOrder.Value,
                            achievement.IsLocking),
                    achievement.UnlockXp,
                    ((AccStarAchievement)achievement).MinStar,
                    ((AccStarAchievement)achievement).RequiredPassCount,
                    achievement.Context.RankedMaps.Count(map =>
                        (achievement.CategoryId == null ||
                         map.Categories.Any(category => category.Id == achievement.CategoryId.Value)) &&
                        map.Rating.AccStar >= ((AccStarAchievement)achievement).MinStar &&
                        (((AccStarAchievement)achievement).MaxStar == null ||
                         map.Rating.AccStar < ((AccStarAchievement)achievement).MaxStar!.Value)),
                    ((AccStarAchievement)achievement).MaxStar == null
                        ? null
                        : ((AccStarAchievement)achievement).MaxStar!.Value.Value);
}