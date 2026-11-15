using Discord;
using Discord.Interactions;
using GuildSaber.Api.Features.Guilds.Members.Http;
using GuildSaber.Common.Helpers;
using GuildSaber.DiscordBot.Core.Handlers;
using static GuildSaber.Api.Features.Guilds.Achievements.Http.AchievementResponses;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [ComponentInteraction("ranking_achievement_delete:*,*,*,*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public async Task ConfirmRankingAchievementDeletionAsync(
        ContextId contextId, int achievementId, int categoryFilter, int page, int categoryPage)
    {
        if (!(await (
                DeferRankingInteractionAsync(),
                Client.Value.Achievements.GetByIdAsync(achievementId)
            ).WhenAll()).TryGetValue(out var achievement, out var error))
        {
            await FollowupAsync(error, ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        if (achievement is not { } existing || existing.ContextId != contextId)
        {
            await ShowRankingAchievementsAsync(contextId, categoryFilter, page, categoryPage);
            await FollowupAsync("This achievement no longer exists in this context.", ephemeral: true);
            return;
        }

        var component = RankingAchievementDeletionView.BuildComponent(existing,
            new RankingAchievementNavigation(categoryFilter, page, categoryPage)).Build();

        await ModifyOriginalResponseAsync(x =>
        {
            x.Flags = MessageFlags.ComponentsV2;
            x.Components = component;
            x.AllowedMentions = AllowedMentions.None;
        });
    }

    [ComponentInteraction("ranking_achievement_delete_confirm:*,*,*,*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public async Task DeleteRankingAchievementAsync(
        ContextId contextId, int achievementId, int categoryFilter, int page, int categoryPage)
    {
        if (!(await (
                DeferRankingInteractionAsync(),
                Client.Value.Achievements.DeleteAsync(contextId, achievementId)
            ).WhenAll()).TryGetValue(out var deleted, out var error))
        {
            await FollowupAsync(error, ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        await ShowRankingAchievementsAsync(contextId, categoryFilter, page, categoryPage);
        await FollowupAsync(deleted
                ? "Achievement deleted. Remaining level progression refreshes in the background."
                : "This achievement has already been deleted.",
            ephemeral: true);
    }
}

file static class RankingAchievementDeletionView
{
    public static ComponentBuilderV2 BuildComponent(Achievement achievement, RankingAchievementNavigation navigation)
    {
        var args = navigation.Arguments(achievement.ContextId, achievement.Id);
        return new ComponentBuilderV2().WithContainer(container => container.WithAccentColor(Color.Red)
            .WithTextDisplay($"### Delete achievement?\n**{Format.Sanitize(achievement.Info.Name)}** · " +
                             $"Achievement **#{achievement.Id}**\n\n" +
                             "This permanently removes its stored member progress and any explicit ranked-map " +
                             "associations. Remaining level progression refreshes in the background.")
            .WithActionRow(row => row
                .WithButton("Cancel", $"ranking_achievement:{args}", ButtonStyle.Secondary)
                .WithButton("Delete achievement", $"ranking_achievement_delete_confirm:{args}", ButtonStyle.Danger)));
    }
}