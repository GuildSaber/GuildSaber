using CSharpFunctionalExtensions;
using Discord;
using Discord.Interactions;
using GuildSaber.Api.Features.Guilds.Members.Http;
using GuildSaber.Common.Helpers;
using GuildSaber.DiscordBot.Core.Extensions;
using GuildSaber.DiscordBot.Core.Handlers;
using static GuildSaber.Api.Features.Guilds.Categories.Http.CategoryResponses;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [ComponentInteraction("ranking_category_delete:*,*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public async Task ConfirmRankingCategoryDeletionAsync(ContextId contextId, CategoryId categoryId, int page)
    {
        if (!(await (
                DeferRankingInteractionAsync(),
                GetRankingCategoriesAsync()
            ).WhenAll()).TryGetValue(out var categories, out var error))
        {
            await FollowupAsync(error, ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        if (!categories.TryFirst(x => x.Id == categoryId).TryGetValue(out var category))
        {
            await ShowRankingCategoriesAsync(contextId, page);
            await FollowupAsync("This category no longer exists in this guild.", ephemeral: true);
            return;
        }

        var component = RankingCategoryDeletionView.BuildComponent(contextId, category, page).Build();
        await ModifyOriginalResponseAsync(x =>
        {
            x.Flags = MessageFlags.ComponentsV2;
            x.Components = component;
            x.AllowedMentions = AllowedMentions.None;
        });
    }

    [ComponentInteraction("ranking_category_delete_confirm:*,*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public async Task DeleteRankingCategoryAsync(ContextId contextId, CategoryId categoryId, int page)
    {
        if (!(await (
                DeferRankingInteractionAsync(),
                DeleteRankingCategoryAsync(categoryId)
            ).WhenAll()).TryGetValue(out var deleted, out var error))
        {
            await FollowupAsync(error, ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        await ShowRankingCategoriesAsync(contextId, page);
        await FollowupAsync(deleted
                ? "Category and its associated achievements deleted."
                : "This category has already been deleted.",
            ephemeral: true);
    }

    private async Task<Result<bool>> DeleteRankingCategoryAsync(CategoryId categoryId)
    {
        var guildId = await GetGuildIdAsync();
        return await Client.Value.Categories.DeleteAsync(guildId, categoryId)
            .Tap(_ => Cache.InvalidateGuildCategoriesAsync(guildId, categoryId).AsTask());
    }
}

file static class RankingCategoryDeletionView
{
    public static ComponentBuilderV2 BuildComponent(ContextId contextId, Category category, int page)
        => new ComponentBuilderV2().WithContainer(container => container.WithAccentColor(Color.Red)
            .WithTextDisplay($"### Delete category?\n**{Format.Sanitize(category.Info.Name)}** · " +
                             $"Category **#{category.Id}**\n\n" +
                             "This permanently deletes the category and its associated achievements across every " +
                             "context in this guild. Ranked-map category associations and category-specific " +
                             "member stats are removed as well.")
            .WithActionRow(row => row
                .WithButton("Cancel", $"ranking_category:{contextId},{category.Id}", ButtonStyle.Secondary)
                .WithButton("Delete category", $"ranking_category_delete_confirm:{contextId},{category.Id},{page}",
                    ButtonStyle.Danger)));
}