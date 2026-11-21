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
    [ComponentInteraction("ranking_achievement:*,*,*,*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public async Task ShowRankingAchievementAsync(
        ContextId contextId, int achievementId, int categoryFilter, int page, int categoryPage)
    {
        var (result, categories, permissions) = await (
            DeferRankingInteractionAsync(),
            Client.Value.Achievements.GetByIdAsync(achievementId),
            Cache.GetGuildCategoriesAsync(await GetGuildIdAsync(), Client.Value).AsTask(),
            Cache.GetUserPermissionsOnDiscordGuildsAsync(Context.User.DiscordId, Services).AsTask()
        ).WhenAll();

        if (!result.TryGetValue(out var achievement, out var error))
        {
            await FollowupAsync(error, ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (achievement is null || achievement.ContextId != contextId)
        {
            await ShowRankingAchievementsAsync(contextId, categoryFilter, page, categoryPage);
            await FollowupAsync("This achievement no longer exists in this context. The list has been refreshed.",
                ephemeral: true);
            return;
        }

        var canUploadCover = permissions.IsManager || permissions.DiscordGuildPermissions
            .GetValueOrDefault(Context.Guild.DiscordId).HasFlag(MemberResponses.EPermission.RankingTeam);
        var component = RankingAchievementView.BuildComponent(achievement, categories,
            new RankingAchievementNavigation(categoryFilter, page, categoryPage),
            Client.Value.Achievements.GetCoverUrl(achievement.Id), canUploadCover).Build();

        await ModifyOriginalResponseAsync(x =>
        {
            x.Flags = MessageFlags.ComponentsV2;
            x.Components = component;
            x.AllowedMentions = AllowedMentions.None;
        });
    }
}

file static class RankingAchievementView
{
    public static ComponentBuilderV2 BuildComponent(
        Achievement achievement, CategoryResponses.Category[] categories, RankingAchievementNavigation navigation,
        Uri coverUrl, bool canUploadCover)
    {
        var args = navigation.Arguments(achievement.ContextId, achievement.Id);
        var categoryName = achievement.GetCategoryName(categories);

        return new ComponentBuilderV2().WithContainer(container => container
                .WithAccentColor(new Color((uint)achievement.Info.Color & 0xFFFFFF))
                .WithSection(section => section
                    .WithTextDisplay($"### {Format.Sanitize(achievement.Info.Name)}\n" +
                                     $"{achievement.KindName} · Achievement **#{achievement.Id}**\n" +
                                     $"Category: **{Format.Sanitize(categoryName)}**")
                    .WithAccessory(new ThumbnailBuilder().WithMedia(coverUrl.ToString())))
                .WithSeparator()
                .WithTextDisplay(achievement switch
                {
                    Achievement.DiffStarAchievement diff =>
                        $"Pass **{diff.RequiredPassCount}** ranked maps with\n" +
                        $"**{diff.MinStar:R} ≤ difficulty stars" +
                        (diff.MaxStar is { } max ? $" < {max:R}**." : "** (no upper limit)."),
                    Achievement.AccStarAchievement acc =>
                        $"Pass **{acc.RequiredPassCount}** ranked maps with\n" +
                        $"**{acc.MinStar:R} ≤ accuracy stars" +
                        (acc.MaxStar is { } max ? $" < {max:R}**." : "** (no upper limit)."),
                    Achievement.RankedMapListAchievement mapList =>
                        $"Pass **{mapList.RequiredPassCount}** associated ranked maps.\n" +
                        "Assign maps through ranked-map proposals."
                })
                .WithTextDisplay(achievement.Progression switch
                                 {
                                     AchievementProgression.Ordered ordered =>
                                         $"**Progression level {ordered.Order}**\n" +
                                         (ordered.IsLocking
                                             ? "Blocks later levels until completed."
                                             : "Completion is optional for later levels."),
                                     AchievementProgression.Unordered =>
                                         "**Standalone achievement**\nIndependent of the level progression."
                                 } + $"\nUnlock XP (reserved): **{achievement.UnlockXp}**\nDiscord role: " +
                                 (achievement.DiscordBindings.RoleId is { } roleId ? $"<@&{roleId}>" : "None"))
                .WithTextDisplay($"**Qualifying maps:** {achievement.QualifyingMapCount}")
                .WithActionRow(row =>
                {
                    row.WithButton("Edit achievement", $"ranking_achievement_edit:{args}");
                    if (canUploadCover)
                        row.WithButton("Upload cover", $"ranking_achievement_cover:{args}", ButtonStyle.Secondary);
                    row.WithButton("Delete achievement", $"ranking_achievement_delete:{args}", ButtonStyle.Danger);
                }))
            .WithActionRow(row => row
                .WithButton("Back", navigation.ListId(achievement.ContextId), ButtonStyle.Secondary)
                .WithButton("Refresh", $"ranking_achievement:{args}", ButtonStyle.Secondary));
    }
}