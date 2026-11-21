using CSharpFunctionalExtensions;
using Discord;
using Discord.Interactions;
using GuildSaber.Api.Shared;
using GuildSaber.DiscordBot.Core.Extensions;

namespace GuildSaber.DiscordBot.Commands.GuildLeader;

public partial class GuildLeaderModuleSlash
{
    public enum EGuildImage
    {
        Logo,
        Banner
    }

    [ComponentInteraction("guild_image:*")]
    public Task OpenGuildImageUploadAsync(EGuildImage image)
        => RespondWithModalAsync<GuildImageUploadModal>($"guild_image_upload:{(int)image}",
            modifyModal: modal =>
            {
                var isBanner = image == EGuildImage.Banner;
                var (width, height) = ImageUploadLimits.GuildBannerAspectRatio;
                var shape = isBanner ? $"{width}:{height} aspect ratio" : "Square";
                modal.WithTitle(isBanner ? "Upload guild banner" : "Upload guild logo");
                modal.Components.OfType<LabelBuilder>().Single()
                    .WithDescription($"{shape}. Up to 2 MiB and {ImageUploadLimits.MaxGuildImageDimension} pixels per side.");
            });

    [ModalInteraction("guild_image_upload:*")]
    public async Task SubmitGuildImageAsync(EGuildImage image, GuildImageUploadModal modal)
    {
        if (await Context.Interaction.ReportImageUploadAsync(
                HttpClientFactory.UploadImageAsync(modal.Image, (module: this, image),
                    static (state, jpeg) => state.module.UploadGuildImageAsync(state.image, jpeg)),
                image == EGuildImage.Banner ? "Guild banner uploaded." : "Guild logo uploaded."))
            await GuildMenuAsync();
    }

    private async Task<Result> UploadGuildImageAsync(EGuildImage image, Stream jpeg)
    {
        var guildId = await GetGuildIdAsync();
        return image == EGuildImage.Banner
            ? await Client.Value.Guilds.UploadBannerAsync(guildId, jpeg)
            : await Client.Value.Guilds.UploadLogoAsync(guildId, jpeg);
    }
}

public class GuildImageUploadModal : IModal
{
    public string Title => "Upload guild image";

    [InputLabel("JPEG image")]
    [ModalFileUpload("image", minValues: 1, maxValues: 1)]
    [ModalFileType(".jpg")]
    [ModalFileType(".jpeg")]
    public IAttachment Image { get; set; } = null!;
}
