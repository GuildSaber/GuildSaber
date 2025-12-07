using Discord;

namespace GuildSaber.DiscordBot.Core.Extensions;

public static class EmbedBuilderExtensions
{
    extension(EmbedBuilder)
    {
        public static int MaxFieldValueLength => 1024;
    }
}