using Discord;
using GuildSaber.Api.Features.Players.Http;

namespace GuildSaber.DiscordBot.Core.Extensions;

public static class PlayerExtensions
{
    extension(ContainerBuilder self)
    {
        public ContainerBuilder WithRequester(PlayerResponses.Player? player, string label = "Requested by")
            => player is null
                ? self
                : self.WithTextDisplay($"-# {label} " +
                                       (player.PlayerLinkedAccounts.DiscordId is { } discordId
                                           ? $"<@{discordId}>"
                                           : Format.Sanitize(player.PlayerInfo.Username)));
    }
}