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
    [ComponentInteraction("ranking_category:*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public async Task ShowRankingCategoryAsync(ContextId contextId, CategoryId categoryId)
    {
        var (result, permissions) = await (
            DeferRankingInteractionAsync(),
            GetRankingCategoriesAsync(),
            Cache.GetUserPermissionsOnDiscordGuildsAsync(Context.User.DiscordId, Services).AsTask()
        ).WhenAll();

        if (!result.TryGetValue(out var categories, out var error))
        {
            await FollowupAsync(error, ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        if (!categories.TryFirst(x => x.Id == categoryId).TryGetValue(out var category))
        {
            await ShowRankingCategoriesAsync(contextId, 1);
            await FollowupAsync("This category no longer exists in this guild. The list has been refreshed.",
                ephemeral: true);
            return;
        }

        var page = categories.OrderBy(x => x.Info.Name, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id.Value)
            .Index().Where(x => x.Item.Id == categoryId).Select(x => x.Index / 25 + 1).First();
        var canUploadLogo = permissions.IsManager || permissions.DiscordGuildPermissions
            .GetValueOrDefault(Context.Guild.DiscordId)
            .HasFlag(MemberResponses.EPermission.RankingTeam);

        var component = RankingCategoryView.BuildComponent(contextId, category, page, Client.Value.Categories
                .GetLogoUrl(category.Id), canUploadLogo)
            .Build();

        await ModifyOriginalResponseAsync(x =>
        {
            x.Flags = MessageFlags.ComponentsV2;
            x.Components = component;
            x.AllowedMentions = AllowedMentions.None;
        });
    }
}

file static class RankingCategoryView
{
    public static ComponentBuilderV2 BuildComponent(
        ContextId contextId, Category category, int page, Uri logoUrl, bool canUploadLogo)
        => new ComponentBuilderV2().WithContainer(container => container.WithAccentColor(Color.Red)
                .WithSection(section => section
                    .WithTextDisplay($"### {Format.Sanitize(category.Info.Name)}\nCategory **#{category.Id}**")
                    .WithAccessory(new ThumbnailBuilder().WithMedia(logoUrl.ToString())))
                .WithSeparator()
                .WithTextDisplay(string.IsNullOrWhiteSpace(category.Info.Description)
                    ? "No description."
                    : Format.Sanitize(category.Info.Description))
                .WithActionRow(row =>
                {
                    row.WithButton("Edit category", $"ranking_category_edit:{contextId},{category.Id}");
                    if (canUploadLogo)
                        row.WithButton("Upload logo", $"ranking_category_logo:{contextId},{category.Id}",
                            ButtonStyle.Secondary);
                    row.WithButton("Delete category", $"ranking_category_delete:{contextId},{category.Id},{page}",
                        ButtonStyle.Danger);
                }))
            .WithActionRow(row => row
                .WithButton("Back", $"ranking_categories:{contextId},{page}", ButtonStyle.Secondary)
                .WithButton("Refresh", $"ranking_category:{contextId},{category.Id}", ButtonStyle.Secondary));
}