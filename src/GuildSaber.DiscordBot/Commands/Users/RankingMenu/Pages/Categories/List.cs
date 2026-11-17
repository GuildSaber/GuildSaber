using CSharpFunctionalExtensions;
using Discord;
using Discord.Interactions;
using GuildSaber.Api.Features.Guilds.Members.Http;
using GuildSaber.Common.Helpers;
using GuildSaber.DiscordBot.Core.Handlers;
using static GuildSaber.Api.Features.Guilds.Categories.Http.CategoryResponses;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [ComponentInteraction("ranking_categories:*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public async Task ShowRankingCategoriesAsync(ContextId contextId, int page)
    {
        if (!(await (
                DeferRankingInteractionAsync(),
                GetRankingCategoriesAsync()
            ).WhenAll()).TryGetValue(out var categories, out var error))
        {
            await FollowupAsync(error, ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        var component = RankingCategoryListView.BuildComponent(contextId, categories,
            Math.Clamp(page, 1, Math.Max(1, (categories.Length + 24) / 25))).Build();

        await ModifyOriginalResponseAsync(x =>
        {
            x.Flags = MessageFlags.ComponentsV2;
            x.Components = component;
            x.AllowedMentions = AllowedMentions.None;
        });
    }

    [ComponentInteraction("ranking_category_pick:*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public Task PickRankingCategoryAsync(ContextId contextId, string[] values)
        => ShowRankingCategoryAsync(contextId, new CategoryId(int.Parse(values[0])));

    private async Task<Result<Category[]>> GetRankingCategoriesAsync()
        => await Client.Value.Categories.GetAllByGuildIdAsync(await GetGuildIdAsync());
}

file static class RankingCategoryListView
{
    public static ComponentBuilderV2 BuildComponent(ContextId contextId, Category[] categories, int page)
    {
        var builder = new ComponentBuilderV2().WithContainer(container => container.WithAccentColor(Color.Red)
            .WithTextDisplay($"### Categories ({categories.Length})\n" +
                             "Categories apply across all contexts in a guild."));

        if (categories.Length == 0)
        {
            builder.WithTextDisplay("There are no categories in this guild yet.");
        }
        else
        {
            var duplicateNames = categories.GroupBy(x => x.Info.Name, StringComparer.OrdinalIgnoreCase)
                .Where(x => x.Count() > 1).Select(x => x.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

            builder.WithActionRow(row => row.WithSelectMenu(new SelectMenuBuilder()
                .WithCustomId($"ranking_category_pick:{contextId}")
                .WithPlaceholder("Choose a category")
                .WithOptions(categories.OrderBy(x => x.Info.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.Id.Value).Skip((page - 1) * 25).Take(25)
                    .Select(x => new SelectMenuOptionBuilder(
                        duplicateNames.Contains(x.Info.Name) ? $"{x.Info.Name} (#{x.Id})" : x.Info.Name,
                        x.Id.ToString(), string.IsNullOrWhiteSpace(x.Info.Description)
                            ? null
                            : x.Info.Description.Trim().Truncate(100))).ToList())));
        }

        return builder.WithTextDisplay($"Page: **{page}**/{Math.Max(1, (categories.Length + 24) / 25)}")
            .WithActionRow(row => row
                .WithButton("New category", $"ranking_category_new:{contextId}", ButtonStyle.Success)
                .WithButton("Back to menu", $"ranking:{contextId}", ButtonStyle.Secondary)
                .WithButton("Prev", $"ranking_categories:{contextId},{page - 1}", disabled: page <= 1)
                .WithButton("Next", $"ranking_categories:{contextId},{page + 1}",
                    disabled: page * 25 >= categories.Length)
                .WithButton("Refresh", $"ranking_categories:{contextId},{page}", ButtonStyle.Secondary));
    }
}