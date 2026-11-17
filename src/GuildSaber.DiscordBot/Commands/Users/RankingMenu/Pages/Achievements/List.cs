using Discord;
using Discord.Interactions;
using GuildSaber.Api.Features.Guilds.Categories.Http;
using GuildSaber.Api.Features.Guilds.Members.Http;
using GuildSaber.Common.Helpers;
using GuildSaber.DiscordBot.Core.Extensions;
using GuildSaber.DiscordBot.Core.Handlers;
using static GuildSaber.Api.Features.Guilds.Achievements.Http.AchievementResponses;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [ComponentInteraction("ranking_achievements:*,*,*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public async Task ShowRankingAchievementsAsync(ContextId contextId, int categoryFilter, int page, int categoryPage)
    {
        var (result, categories) = await (
            DeferRankingInteractionAsync(),
            Client.Value.Achievements.GetByContextIdAsync(contextId,
                categoryFilter > 0 ? categoryFilter : null, hasCategory: categoryFilter != 0),
            Cache.GetGuildCategoriesAsync(await GetGuildIdAsync(), Client.Value).AsTask()
        ).WhenAll();

        if (!result.TryGetValue(out var achievements, out var error))
        {
            await FollowupAsync(error, ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        var navigation = new RankingAchievementNavigation(categoryFilter,
            Math.Clamp(page, 1, Math.Max(1, (achievements.Length + 24) / 25)),
            Math.Clamp(categoryPage, 1, Math.Max(1, (categories.Length + 22) / 23)));
        var component = RankingAchievementListView.BuildComponent(contextId, achievements, categories, navigation)
            .Build();

        await ModifyOriginalResponseAsync(x =>
        {
            x.Flags = MessageFlags.ComponentsV2;
            x.Components = component;
            x.AllowedMentions = AllowedMentions.None;
        });
    }

    [ComponentInteraction("ranking_achievements_category:*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public Task FilterRankingAchievementsAsync(ContextId contextId, int categoryPage, string[] values)
        => ShowRankingAchievementsAsync(contextId, int.Parse(values[0]), 1, categoryPage);

    [ComponentInteraction("ranking_achievement_pick:*,*,*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public Task PickRankingAchievementAsync(
        ContextId contextId, int categoryFilter, int page, int categoryPage, string[] values)
        => ShowRankingAchievementAsync(contextId, int.Parse(values[0]), categoryFilter, page, categoryPage);
}

public readonly record struct RankingAchievementNavigation(int CategoryFilter, int Page, int CategoryPage)
{
    public string Arguments(ContextId contextId, int achievementId)
        => $"{contextId},{achievementId},{CategoryFilter},{Page},{CategoryPage}";

    public string ListId(ContextId contextId)
        => $"ranking_achievements:{contextId},{CategoryFilter},{Page},{CategoryPage}";
}

file static class RankingAchievementListView
{
    public static ComponentBuilderV2 BuildComponent(
        ContextId contextId, Achievement[] achievements, CategoryResponses.Category[] categories,
        RankingAchievementNavigation navigation)
    {
        var categoryName = navigation.CategoryFilter switch
        {
            -1 => "All categories",
            0 => "Global",
            var id => categories.Where(x => x.Id.Value == id).Select(x => x.Info.Name)
                .DefaultIfEmpty("Deleted category").First()
        };
        var pageCount = Math.Max(1, (achievements.Length + 24) / 25);
        var builder = new ComponentBuilderV2().WithContainer(container => container.WithAccentColor(Color.Red)
            .WithTextDisplay("### Manage achievements\n" +
                             $"**{achievements.Length}** achievements")
            .WithActionRow(row => row.WithSelectMenu(new SelectMenuBuilder()
                .WithCustomId($"ranking_achievements_category:{contextId},{navigation.CategoryPage}")
                .WithPlaceholder($"Category: {categoryName}".Truncate(150))
                .WithOptions([
                    new SelectMenuOptionBuilder("All categories", "-1", "Includes global achievements",
                        isDefault: navigation.CategoryFilter == -1),
                    new SelectMenuOptionBuilder("Global", "0", "Achievements without a category",
                        isDefault: navigation.CategoryFilter == 0),
                    .. categories.OrderBy(x => x.Info.Name, StringComparer.OrdinalIgnoreCase)
                        .Skip((navigation.CategoryPage - 1) * 23).Take(23)
                        .Select(x => new SelectMenuOptionBuilder(x.Info.Name.Truncate(100), x.Id.ToString(),
                            isDefault: navigation.CategoryFilter == x.Id.Value))
                ]))));

        if (categories.Length > 23)
            builder.WithActionRow(row => row
                .WithButton("Prev categories",
                    (navigation with { CategoryPage = navigation.CategoryPage - 1 }).ListId(contextId),
                    ButtonStyle.Secondary, disabled: navigation.CategoryPage <= 1)
                .WithButton("Next categories",
                    (navigation with { CategoryPage = navigation.CategoryPage + 1 }).ListId(contextId),
                    ButtonStyle.Secondary, disabled: navigation.CategoryPage * 23 >= categories.Length));

        if (achievements.Length == 0)
            builder.WithTextDisplay(navigation.CategoryFilter switch
            {
                -1 => "No achievements in this context.",
                0 => "No global achievements.",
                _ => $"No achievements in **{Format.Sanitize(categoryName)}**."
            });
        else
            builder.WithActionRow(row => row.WithSelectMenu(new SelectMenuBuilder()
                .WithCustomId("ranking_achievement_pick:" +
                              $"{contextId},{navigation.CategoryFilter},{navigation.Page},{navigation.CategoryPage}")
                .WithPlaceholder("Choose an achievement")
                .WithOptions(achievements.OrderBy(x => x.Progression is not AchievementProgression.Ordered)
                    .ThenBy(x => (x.Progression as AchievementProgression.Ordered)?.Order)
                    .ThenBy(x => x.Info.Name, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id)
                    .Skip((navigation.Page - 1) * 25).Take(25)
                    .Select(x => new SelectMenuOptionBuilder(x.Info.Name.Truncate(100), x.Id.ToString(),
                        ($"{x.GetCategoryName(categories)} · " +
                         (x.Progression is AchievementProgression.Ordered ordered
                             ? $"Level {ordered.Order}"
                             : "Standalone") +
                         $" · {x.KindName} · " +
                         $"{x.QualifyingMapCount} qualifying {(x.QualifyingMapCount == 1 ? "map" : "maps")}")
                        .Truncate(100))).ToList())));

        return builder.WithTextDisplay($"Page: **{navigation.Page}**/{pageCount}")
            .WithActionRow(row => row
                .WithButton("New achievement",
                    $"ranking_achievement_new:{contextId},{navigation.CategoryFilter},{navigation.Page}," +
                    $"{navigation.CategoryPage}",
                    ButtonStyle.Success)
                .WithButton("Back to menu", $"ranking:{contextId}", ButtonStyle.Secondary)
                .WithButton("Prev",
                    (navigation with { Page = navigation.Page - 1 }).ListId(contextId),
                    disabled: navigation.Page <= 1)
                .WithButton("Next",
                    (navigation with { Page = navigation.Page + 1 }).ListId(contextId),
                    disabled: navigation.Page >= pageCount)
                .WithButton("Refresh", navigation.ListId(contextId), ButtonStyle.Secondary));
    }
}