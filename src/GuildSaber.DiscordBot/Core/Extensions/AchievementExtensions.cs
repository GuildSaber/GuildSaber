using System.Globalization;
using Discord;
using GuildSaber.Api.Features.Guilds.Categories.Http;
using static GuildSaber.Api.Features.Guilds.Achievements.Http.AchievementRequests;
using static GuildSaber.Api.Features.Guilds.Achievements.Http.AchievementResponses;

namespace GuildSaber.DiscordBot.Core.Extensions;

public static class AchievementExtensions
{
    extension(Achievement self)
    {
        public int QualifyingMapCount => self switch
        {
            Achievement.DiffStarAchievement diff => diff.TotalCount,
            Achievement.AccStarAchievement acc => acc.TotalCount,
            Achievement.RankedMapListAchievement mapList => mapList.TotalCount
        };

        public string KindName => self switch
        {
            Achievement.RankedMapListAchievement => "Map list",
            Achievement.DiffStarAchievement => "Difficulty stars",
            Achievement.AccStarAchievement => "Accuracy stars"
        };

        public Definition ToDefinition() => new(
            Name: self.Info.Name,
            Color: self.Info.Color,
            CategoryId: self.CategoryId,
            RoleId: self.DiscordBindings.RoleId,
            ProgressionOrder: (self.Progression as AchievementProgression.Ordered)?.Order,
            IsLocking: self.Progression is AchievementProgression.Ordered { IsLocking: true },
            UnlockXp: self.UnlockXp,
            Requirement: self switch
            {
                Achievement.RankedMapListAchievement mapList => new Requirement.RankedMapList(
                    mapList.RequiredPassCount),
                Achievement.DiffStarAchievement diff => new Requirement.DiffStar(
                    diff.MinStar, diff.RequiredPassCount, diff.MaxStar),
                Achievement.AccStarAchievement acc => new Requirement.AccStar(
                    acc.MinStar, acc.RequiredPassCount, acc.MaxStar)
            });

        public string GetCategoryName(IEnumerable<CategoryResponses.Category> categories)
            => self.CategoryId is { } categoryId
                ? categories.Where(x => x.Id == categoryId).Select(x => x.Info.Name)
                    .DefaultIfEmpty("Deleted category").First()
                : "Global";
    }

    extension(Definition self)
    {
        public string ProgressionDescription => self.ProgressionOrder is null
            ? "Earned independently of the level progression."
            : "Shown as a level on player cards and in /progress.";

        public string GetCategoryName(IEnumerable<CategoryResponses.Category> categories)
            => self.CategoryId is { } categoryId
                ? categories.Where(x => x.Id == categoryId).Select(x => x.Info.Name)
                    .DefaultIfEmpty("Deleted category").First()
                : "Global";

        public string DescribeValues(
            Definition saved, IReadOnlyCollection<CategoryResponses.Category> categories, bool onlyChanges = false)
            => string.Join("\n", new (bool Changed, string Field, string Before, string After)[]
                {
                    (saved.Name != self.Name, "Name", Format.Sanitize(saved.Name),
                        string.IsNullOrWhiteSpace(self.Name) ? "Not set" : Format.Sanitize(self.Name)),
                    (saved.Color != self.Color, "Color", $"#{(uint)saved.Color:X8}", $"#{(uint)self.Color:X8}"),
                    (saved.Requirement != self.Requirement, "Completion rule",
                        saved.Requirement.Summary, self.Requirement.Summary),
                    (saved.CategoryId != self.CategoryId, "Category",
                        Format.Sanitize(saved.GetCategoryName(categories)) +
                        (saved.CategoryId is { } oldCategory ? $" (#{oldCategory})" : ""),
                        Format.Sanitize(self.GetCategoryName(categories)) +
                        (self.CategoryId is { } newCategory ? $" (#{newCategory})" : "")),
                    (saved.RoleId != self.RoleId, "Discord role",
                        saved.RoleId is { } oldRole ? $"<@&{oldRole}>" : "None",
                        self.RoleId is { } newRole ? $"<@&{newRole}>" : "None"),
                    (saved.ProgressionOrder.HasValue != self.ProgressionOrder.HasValue, "Display mode",
                        saved.ProgressionOrder.HasValue ? "Progression level" : "Standalone achievement",
                        self.ProgressionOrder.HasValue ? "Progression level" : "Standalone achievement"),
                    (saved.ProgressionOrder != self.ProgressionOrder, "Level order",
                        saved.ProgressionOrder?.ToString(CultureInfo.InvariantCulture) ?? "None",
                        self.ProgressionOrder?.ToString(CultureInfo.InvariantCulture) ?? "None"),
                    (saved.IsLocking != self.IsLocking, "Blocks later levels",
                        saved.IsLocking ? "On" : "Off", self.IsLocking ? "On" : "Off"),
                    (saved.UnlockXp != self.UnlockXp, "Unlock XP (reserved)",
                        saved.UnlockXp.ToString(), self.UnlockXp.ToString())
                }
                .Where(change => !onlyChanges || change.Changed)
                .Select(static change => change.Changed
                    ? $"**{change.Field}** {change.Before} → {change.After}"
                    : $"**{change.Field}** {change.After}"));
    }

    extension(Requirement self)
    {
        public string Summary => self switch
        {
            Requirement.DiffStar diff =>
                $"{diff.MinStar:R} ≤ difficulty stars" +
                (diff.MaxStar is { } max ? $" < {max:R}" : " (unbounded)") +
                $" · {diff.RequiredPassCount} passes",
            Requirement.AccStar acc =>
                $"{acc.MinStar:R} ≤ accuracy stars" +
                (acc.MaxStar is { } max ? $" < {max:R}" : " (unbounded)") +
                $" · {acc.RequiredPassCount} passes",
            Requirement.RankedMapList list => $"{list.RequiredPassCount} passes from the associated map list"
        };
    }
}