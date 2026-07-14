using Discord;

namespace GuildSaber.DiscordBot.Core.Extensions;

public static class UserMessageExtensions
{
    extension(IUserMessage self)
    {
        public string Link => $"https://discord.com/channels/" +
                              $"{((IGuildChannel)self.Channel).GuildId}/{self.Channel.Id}/{self.Id}";
    }
}