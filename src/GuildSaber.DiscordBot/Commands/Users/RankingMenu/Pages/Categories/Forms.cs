using CSharpFunctionalExtensions;
using Discord;
using Discord.Interactions;
using GuildSaber.Api.Features.Guilds.Categories.Http;
using GuildSaber.Api.Features.Guilds.Members.Http;
using GuildSaber.Common.Helpers;
using GuildSaber.DiscordBot.Core.Extensions;
using GuildSaber.DiscordBot.Core.Handlers;
using static GuildSaber.Api.Features.Guilds.Categories.Http.CategoryResponses;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [ComponentInteraction("ranking_category_new:*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public Task NewRankingCategoryAsync(ContextId contextId)
        => RespondWithModalAsync($"ranking_category_create:{contextId}",
            new RankingCategoryModal { Title = "Create category" });

    [ModalInteraction("ranking_category_create:*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public Task CreateRankingCategoryAsync(ContextId contextId, RankingCategoryModal modal)
        => SaveRankingCategoryAsync(contextId, null, modal);

    [ComponentInteraction("ranking_category_edit:*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public async Task EditRankingCategoryAsync(ContextId contextId, CategoryId categoryId)
    {
        var (guildId, result) = await (
            GetGuildIdAsync().AsTask(),
            Client.Value.Categories.GetByIdAsync(categoryId)
        ).WhenAll();

        if (!result.TryGetValue(out var category, out var error))
        {
            await RespondAsync(error, ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        if (category is not { } existing || existing.GuildId != guildId)
        {
            await RespondAsync("This category no longer exists in this guild. Refresh the list to continue.",
                ephemeral: true);
            return;
        }

        await RespondWithModalAsync($"ranking_category_update:{contextId},{categoryId}", new RankingCategoryModal
        {
            Name = existing.Info.Name,
            Description = existing.Info.Description
        });
    }

    [ModalInteraction("ranking_category_update:*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public Task UpdateRankingCategoryAsync(ContextId contextId, CategoryId categoryId, RankingCategoryModal modal)
        => SaveRankingCategoryAsync(contextId, categoryId, modal);

    private async Task SaveRankingCategoryAsync(ContextId contextId, CategoryId? categoryId, RankingCategoryModal modal)
    {
        if (modal.Name.Trim().Length < 2 || string.IsNullOrWhiteSpace(modal.Description))
        {
            await RespondAsync("Enter a name with at least 2 characters and a description.", ephemeral: true);
            return;
        }

        if (!(await (
                DeferRankingInteractionAsync(),
                SaveRankingCategoryAsync(categoryId, modal)
            ).WhenAll()).TryGetValue(out var category, out var error))
        {
            await FollowupAsync(error, ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        await Cache.InvalidateGuildCategoriesAsync(category.GuildId, category.Id);
        await ShowRankingCategoryAsync(contextId, category.Id);
        await FollowupAsync(categoryId is null ? "Category created." : "Category updated.", ephemeral: true);
    }

    private async Task<Result<Category>> SaveRankingCategoryAsync(
        CategoryId? categoryId, RankingCategoryModal modal)
        => categoryId is { } id
            ? await Client.Value.Categories.UpdateAsync(await GetGuildIdAsync(), id,
                new CategoryRequests.UpdateCategory(modal.Name.Trim(), modal.Description.Trim()))
            : await Client.Value.Categories.CreateAsync(await GetGuildIdAsync(),
                new CategoryRequests.CreateCategory(modal.Name.Trim(), modal.Description.Trim()));
}

public class RankingCategoryModal : IModal
{
    public string Title { get; set; } = "Edit category";

    [InputLabel("Name", "Between 2 and 50 characters.")]
    [ModalTextInput("name", minLength: 2, maxLength: 50)]
    public string Name { get; set; } = "";

    [InputLabel("Description", "Up to 256 characters. Submitting this form applies the changes immediately.")]
    [ModalTextInput("description", TextInputStyle.Paragraph, minLength: 1, maxLength: 256)]
    public string Description { get; set; } = "";
}