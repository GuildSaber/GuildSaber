using Discord;
using GuildSaber.Database.Models.StrongTypes;

namespace GuildSaber.DiscordBot.Core.Extensions;

public static class IdExtensions
{
    extension(IUser self)
    {
        public DiscordId DiscordId => DiscordId.CreateUnsafe(self.Id).Value;
    }

    extension(IGuild self)
    {
        public DiscordGuildId DiscordId => DiscordGuildId.CreateUnsafe(self.Id).Value;
    }
}