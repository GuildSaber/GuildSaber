using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.Guilds.Http;
using GuildSaber.Api.Features.Players.Http;
using GuildSaber.Common.StrongTypes;
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

    extension(GuildResponses.GuildExtended? self)
    {
        public GuildResponses.GuildExtended ValueOrGuildMissingException()
            => self ?? throw new InteractionHandler.GuildMissingException();
    }

    extension(PlayerResponses.Player? self)
    {
        public PlayerResponses.Player ValueOrPlayerNotFoundException()
            => self ?? throw new InteractionHandler.PlayerNotFoundException();

        public PlayerResponses.Player ValueOrCurrentPlayerNotRegisteredException()
            => self ?? throw new InteractionHandler.CurrentPlayerNotRegisteredException();
    }

    extension(PlayerResponses.PlayerExtended? self)
    {
        public PlayerResponses.PlayerExtended ValueOrPlayerNotFoundException()
            => self ?? throw new InteractionHandler.PlayerNotFoundException();

        public PlayerResponses.PlayerExtended ValueOrCurrentPlayerNotRegisteredException()
            => self ?? throw new InteractionHandler.CurrentPlayerNotRegisteredException();
    }

    extension<T>(Result<T> self)
    {
        public T UnwrapOrPlayerIsNotInGuildContextException()
            => self.IsSuccess ? self.Value : throw new InteractionHandler.PlayerIsNotInGuildContextException();

        public T UnwrapOrCurrentPlayerDidNotJoinGuildContextException()
            => self.IsSuccess
                ? self.Value
                : throw new InteractionHandler.CurrentPlayerDidNotJoinGuildContextException();
    }

    extension(PlayerId? self)
    {
        public PlayerId ValueOrPlayerNotFoundException()
            => self ?? throw new InteractionHandler.PlayerNotFoundException();
    }
}