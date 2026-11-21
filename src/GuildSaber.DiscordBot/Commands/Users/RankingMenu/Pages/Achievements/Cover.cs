using Discord.Interactions;
using GuildSaber.Api.Features.Guilds.Members.Http;
using GuildSaber.DiscordBot.Core.Extensions;
using GuildSaber.DiscordBot.Core.Handlers;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [ComponentInteraction("ranking_achievement_cover:*,*,*,*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(MemberResponses.EPermission.RankingTeam)]
    public Task OpenRankingAchievementCoverUploadAsync(
        ContextId contextId, int achievementId, int categoryFilter, int page, int categoryPage)
        => RespondWithModalAsync<RankingPngUploadModal>(
            $"ranking_achievement_cover_upload:{contextId},{achievementId},{categoryFilter},{page},{categoryPage}",
            modifyModal: modal => modal.WithTitle("Upload achievement cover"));

    [ModalInteraction("ranking_achievement_cover_upload:*,*,*,*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(MemberResponses.EPermission.RankingTeam)]
    public async Task UploadRankingAchievementCoverAsync(
        ContextId contextId, int achievementId, int categoryFilter, int page, int categoryPage,
        RankingPngUploadModal modal)
    {
        if (await Context.Interaction.ReportImageUploadAsync(
                HttpClientFactory.UploadImageAsync(modal.Image, (Client.Value.Achievements, contextId, achievementId),
                    static (state, png) => state.Achievements
                        .UploadCoverAsync(state.contextId, state.achievementId, png)),
                "Achievement cover uploaded."))
            await ShowRankingAchievementAsync(contextId, achievementId, categoryFilter, page, categoryPage);
    }
}
