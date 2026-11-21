using Discord;
using Discord.Interactions;
using GuildSaber.Common.Helpers;

namespace GuildSaber.DiscordBot.Commands.GuildLeader;

public partial class GuildLeaderModuleSlash
{
    [SlashCommand("guild", "Open the guild settings menu")]
    public async Task GuildMenuAsync()
    {
        var guildId = await (Context.Interaction.HasResponded ? Task.CompletedTask : DeferAsync(ephemeral: true),
            GetGuildIdAsync().AsTask()).WhenAll();

        var component = new ComponentBuilderV2()
            .WithContainer(container => container
                .WithSection(header => header
                    .WithTextDisplay("### Guild settings")
                    .WithAccessory(new ThumbnailBuilder().WithMedia(Client.Value.Guilds.GetLogoUrl(guildId).ToString())))
                .WithMediaGallery(gallery => gallery
                    .AddItem(Client.Value.Guilds.GetGuildBannerUrl(guildId).ToString(), description: "Guild banner"))
                .WithSeparator()
                .WithActionRow(row => row
                    .WithButton("Upload guild logo", $"guild_image:{(int)EGuildImage.Logo}", ButtonStyle.Secondary)
                    .WithButton("Upload guild banner", $"guild_image:{(int)EGuildImage.Banner}", ButtonStyle.Secondary)))
            .Build();

        await ModifyOriginalResponseAsync(x =>
        {
            x.Flags = MessageFlags.ComponentsV2;
            x.Components = component;
            x.AllowedMentions = AllowedMentions.None;
        });
    }
}
