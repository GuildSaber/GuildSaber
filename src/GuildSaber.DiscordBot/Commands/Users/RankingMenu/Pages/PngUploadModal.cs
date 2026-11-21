using Discord;
using Discord.Interactions;

namespace GuildSaber.DiscordBot.Commands.Users;

public class RankingPngUploadModal : IModal
{
    public string Title => "Upload PNG image";

    [InputLabel("PNG image", "Square. Up to 2 MiB and 1024 pixels per side.")]
    [ModalFileUpload("image", minValues: 1, maxValues: 1)]
    [ModalFileType(".png")]
    public IAttachment Image { get; set; } = null!;
}
