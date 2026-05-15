using GuildSaber.Api.Features.Guilds.Categories;
using GuildSaber.Api.Features.Guilds.Members.LevelStats;
using GuildSaber.CSharpClient.Routes.Guilds.Members.LevelStats;
using GuildSaber.DiscordBot.Core.Extensions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GuildSaber.DiscordBot.Commands.Users.Me.Components;

public sealed class CategoryGrid(
    CategoryResponses.Category[] categories,
    LevelStatResponses.MemberLevelStat[] levelStats) : IComponent
{
    public void Compose(IContainer container)
    {
        var categoryLevels = categories
            .Select(category => (category, level: levelStats.GetCategoryLevel(category.Id)))
            .Select(tuple => new CategoryLevelData(
                tuple.category.Info.Name,
                tuple.level?.Info.Name ?? "None",
                Color.FromArgb(tuple.level?.Info.Color ?? 0xFFFFFF)))
            .ToArray();

        container.Column(categoriesCol =>
        {
            foreach (var pair in categoryLevels.Chunk(2))
                categoriesCol.Item()
                    .PaddingHorizontal(25)
                    .PaddingVertical(3)
                    .Row(pairRow =>
                    {
                        pairRow.RelativeItem().Element(c => CategoryCell(c, pair[0]));

                        var secondItem = pairRow.RelativeItem();
                        if (pair.Length > 1)
                            secondItem.Element(c => CategoryCell(c, pair[1]));
                    });
        });
    }

    public record struct CategoryLevelData(string CategoryName, string LevelName, Color Color);

    private static void CategoryCell(IContainer container, CategoryLevelData category) =>
        container.Row(catRow =>
        {
            catRow.RelativeItem()
                .AlignRight()
                .Text($"{category.CategoryName}: ")
                .FontColor(Colors.White)
                .FontSize(26)
                .Bold();
            catRow.RelativeItem()
                .AlignLeft()
                .Text(category.LevelName)
                .FontColor(category.Color)
                .FontSize(26)
                .Bold();
        });
}