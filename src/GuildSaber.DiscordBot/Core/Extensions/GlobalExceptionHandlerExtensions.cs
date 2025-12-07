using GuildSaber.Api.Features.Guilds;
using GuildSaber.Api.Features.Players;
using GuildSaber.Database.Models.StrongTypes;
using GuildSaber.DiscordBot.Core.Handlers;

namespace GuildSaber.DiscordBot.Core.Extensions;

public static class GlobalExceptionExtensions
{
    extension(GuildId? self)
    {
        public GuildId ValueOrGuildMissingException()
            => self ?? throw new InteractionHandler.GuildMissingException();
    }

    extension(DiscordGuildId? self)
    {
        public DiscordGuildId ValueOrGuildMissingException()
            => self ?? throw new InteractionHandler.GuildMissingException();
    }

    extension(DiscordId? self)
    {
        public DiscordId ValueOrPlayerNotFoundException()
            => self ?? throw new InteractionHandler.PlayerNotFoundException();
    }

    extension(GuildResponses.Guild? self)
    {
        public GuildResponses.Guild ValueOrGuildMissingException()
            => self ?? throw new InteractionHandler.GuildMissingException();
    }

    extension(PlayerResponses.Player? self)
    {
        public PlayerResponses.Player ValueOrPlayerNotFoundException()
            => self ?? throw new InteractionHandler.PlayerNotFoundException();
    }

    extension(PlayerResponses.PlayerExtended? self)
    {
        public PlayerResponses.PlayerExtended ValueOrPlayerNotFoundException()
            => self ?? throw new InteractionHandler.PlayerNotFoundException();
    }
}