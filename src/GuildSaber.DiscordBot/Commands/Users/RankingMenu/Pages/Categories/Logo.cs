using Discord.Interactions;
using GuildSaber.Api.Features.Guilds.Members.Http;
using GuildSaber.DiscordBot.Core.Extensions;
using GuildSaber.DiscordBot.Core.Handlers;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [ComponentInteraction("ranking_category_logo:*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(MemberResponses.EPermission.RankingTeam)]
    public Task OpenRankingCategoryLogoUploadAsync(ContextId contextId, CategoryId categoryId)
        => RespondWithModalAsync<RankingPngUploadModal>($"ranking_category_logo_upload:{contextId},{categoryId}",
            modifyModal: modal => modal.WithTitle("Upload category logo"));

    [ModalInteraction("ranking_category_logo_upload:*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(MemberResponses.EPermission.RankingTeam)]
    public async Task UploadRankingCategoryLogoAsync(
        ContextId contextId, CategoryId categoryId, RankingPngUploadModal modal)
    {
        if (await Context.Interaction.ReportImageUploadAsync(
                HttpClientFactory.UploadImageAsync(modal.Image, (module: this, categoryId),
                    static async (state, png) => await state.module.Client.Value.Categories
                        .UploadLogoAsync(await state.module.GetGuildIdAsync(), state.categoryId, png)),
                "Category logo uploaded."))
            await ShowRankingCategoryAsync(contextId, categoryId);
    }
}
