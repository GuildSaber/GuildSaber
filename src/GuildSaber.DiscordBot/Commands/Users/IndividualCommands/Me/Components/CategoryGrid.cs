using GuildSaber.Api.Features.Guilds.Categories.Http;
using GuildSaber.Api.Features.Guilds.Members.AchievementStats.Http;
using GuildSaber.CSharpClient.Routes.Guilds.Members.AchievementStats;
using GuildSaber.DiscordBot.Core.Extensions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GuildSaber.DiscordBot.Commands.Users.Me.Components;

public sealed class CategoryGrid(
    CategoryResponses.Category[] categories,
    AchievementStatResponses.MemberAchievementStat[] achievementStats) : IComponent
{
    public record struct CategoryAchievementData(string CategoryName, string AchievementName, Color Color);

    public void Compose(IContainer container)
    {
        var categoryAchievements = categories
            .Select(category => (category,
                achievement: achievementStats.GetCategoryAchievement(category.Id)))
            .Select(tuple => new CategoryAchievementData(
                tuple.category.Info.Name,
                tuple.achievement?.Info.Name ?? "None",
                Color.FromArgb(tuple.achievement?.Info.Color ?? 0xFFFFFF)))
            .ToArray();

        container.Column(categoriesCol =>
        {
            foreach (var pair in categoryAchievements.Chunk(2))
                categoriesCol.Item()
                    .PaddingVertical(3)
                    .ScaleToFit()
                    .Row(pairRow =>
                    {
                        pairRow.RelativeItem().Element(c => CategoryCell(c, pair[0]));

                        var secondItem = pairRow.RelativeItem();
                        if (pair.Length > 1)
                            secondItem.Element(c => CategoryCell(c, pair[1]));
                    });
        });
    }

    private static void CategoryCell(IContainer container, CategoryAchievementData category) =>
        container.Row(catRow =>
        {
            catRow.RelativeItem()
                .AlignRight()
                .Text($"{category.CategoryName}:")
                .FontColor(Colors.White)
                .FontSize(26)
                .Bold();
            catRow.RelativeItem()
                .AlignLeft()
                .Text(category.AchievementName)
                .FontColor(category.Color)
                .FontSize(26)
                .Bold();
        });
}