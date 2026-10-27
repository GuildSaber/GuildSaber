using GuildSaber.Api.Features.Guilds.Categories.Http;
using GuildSaber.Api.Features.Guilds.Members.AchievementStats.Http;
using GuildSaber.CSharpClient.Routes.Guilds.Members.AchievementStats;
using GuildSaber.DiscordBot.Core.Extensions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static GuildSaber.Api.Features.Guilds.Achievements.Http.AchievementResponses;

namespace GuildSaber.DiscordBot.Commands.Users.Me.Components;

public sealed class CategoryGrid(
    CategoryResponses.Category[] categories,
    AchievementStatResponses.MemberAchievementStat[] achievementStats) : IComponent
{
    public readonly record struct CategoryAchievementData(
        string CategoryName,
        string AchievementName,
        uint Order,
        Color Color
    );

    public void Compose(IContainer container)
    {
        var categoryAchievements = categories
            .Select(category => (category, achievement: achievementStats.GetCategoryAchievement(category.Id)))
            .Select(x => new CategoryAchievementData(
                x.category.Info.Name,
                x.achievement?.Info.Name ?? "None",
                x.achievement?.Progression is AchievementProgression.Ordered progression ? progression.Order : 0,
                Color.FromArgb(x.achievement?.Info.Color ?? 0xFFFFFF)))
            .ToArray();

        var average = categoryAchievements.Length == 0 ? 0 : categoryAchievements.Average(x => x.Order);
        var averageColor = GetAverageColor(average);

        var equilibrium = achievementStats.CalculateSkillEquilibrium(categories.Select(x => x.Id)) ?? 100;
        if (!double.IsFinite(equilibrium)) equilibrium = 100;

        container.Row(row =>
        {
            row.RelativeItem(30).AlignMiddle()
                .Element(column => CategoryColumn(column, categoryAchievements.Where((_, index) => index % 2 == 0)));

            row.RelativeItem(24).AlignMiddle().Column(summary =>
            {
                summary.Item().AlignCenter().Text($"Avg {average:0.##}").FontSize(30).Bold().Italic()
                    .FontColor(averageColor);

                summary.Item().AlignCenter().Text(dots =>
                {
                    for (var index = 0; index < 5; index++)
                        dots.Span(index == 0 ? "●" : " ●").FontSize(24)
                            .FontColor(index < Math.Clamp((int)Math.Round(equilibrium / 20), 0, 5)
                                ? averageColor
                                : Colors.Grey.Darken2);
                });

                summary.Item().AlignCenter().Text($"({equilibrium:0.##}%)").FontSize(24).FontColor(averageColor);
            });

            row.RelativeItem(30).AlignMiddle()
                .Element(column => CategoryColumn(column, categoryAchievements.Where((_, index) => index % 2 != 0)));
        });
    }

    private static void CategoryColumn(IContainer container, IEnumerable<CategoryAchievementData> categories)
        => container.Column(column =>
        {
            foreach (var category in categories)
                column.Item().Height(45).AlignMiddle().AlignCenter().ScaleToFit().Text(text =>
                {
                    text.Span($"{category.CategoryName}: ").FontSize(26);
                    text.Span(category.AchievementName).FontSize(26).Bold().FontColor(category.Color);
                });
        });

    private Color GetAverageColor(double average)
    {
        var achievements = achievementStats.Select(x => x.Achievement)
            .Where(x => x.CategoryId is null && x.Progression is AchievementProgression.Ordered)
            .OrderBy(x => ((AchievementProgression.Ordered)x.Progression).Order)
            .ToArray();

        var (lower, upper) = (
            achievements.LastOrDefault(x => ((AchievementProgression.Ordered)x.Progression).Order <= average),
            achievements.FirstOrDefault(x => ((AchievementProgression.Ordered)x.Progression).Order >= average)
        );

        if (lower is null) return Color.FromArgb(upper?.Info.Color ?? 0xFFFFFF);
        if (upper is null) return Color.FromArgb(lower.Info.Color);

        var lowerOrder = ((AchievementProgression.Ordered)lower.Progression).Order;
        var upperOrder = ((AchievementProgression.Ordered)upper.Progression).Order;
        if (lowerOrder == upperOrder) return Color.FromArgb(lower.Info.Color);

        var start = System.Drawing.Color.FromArgb(lower.Info.Color);
        var end = System.Drawing.Color.FromArgb(upper.Info.Color);
        var blend = (average - lowerOrder) / (upperOrder - lowerOrder);

        return Color.FromRGB(
            (byte)Math.Round(start.R + (end.R - start.R) * blend),
            (byte)Math.Round(start.G + (end.G - start.G) * blend),
            (byte)Math.Round(start.B + (end.B - start.B) * blend)
        );
    }
}