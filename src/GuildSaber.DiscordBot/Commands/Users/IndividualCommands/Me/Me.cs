using Discord;
using Discord.Interactions;
using GuildSaber.Common.Helpers;
using GuildSaber.Common.Result;
using GuildSaber.DiscordBot.AutocompleteHandlers;
using GuildSaber.DiscordBot.Commands.Users.Me;
using GuildSaber.DiscordBot.Core.Handlers;
using QuestPDF.Fluent;

// ReSharper disable CheckNamespace
namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [SlashCommand("me", "Get information about your account")]
    public async Task Me(
        [Autocomplete<ContextAutocompleteHandler>] int contextId,
        [Summary("User", "The user to get the player card for (you if empty)")] IUser? user = null,
        [Summary("Visibility")] EDisplayChoice displayChoice = EDisplayChoice.Visible)
    {
        await DeferAsync(ephemeral: displayChoice.ToEphemeral());
        var (guildExtended, player) = await (GetGuildExtendedAsync().AsTask(), GetPlayerAsync(user).AsTask())
            .WhenAll();

        var client = Client.Value;
        var (levelStats, contextStats, avatarBytes, guildLogoBytes) = await (
                client.LevelStats.GetByPlayerIdAsync(player.Id, contextId),
                client.ContextStats.GetByPlayerIdAsync(player.Id, contextId),
                client.HttpClient.GetByteArrayAsync(player.PlayerInfo.AvatarUrl),
                client.HttpClient.GetByteArrayAsync(client.Guilds.GetLogoUrl(guildExtended.Guild.Id)))
            .WhenAll();

        var playerCardImageByte = new PlayerCardView(
                player,
                guildExtended,
                levelStats.Unwrap(),
                contextStats.Unwrap() ?? (user is null
                    ? throw new InteractionHandler.CurrentPlayerDidNotJoinGuildContextException()
                    : throw new InteractionHandler.PlayerIsNotInGuildContextException()),
                avatarBytes: avatarBytes,
                guildLogoBytes: guildLogoBytes)
            .GenerateImages().First();

        using var stream = new MemoryStream();
        await stream.WriteAsync(playerCardImageByte);
        stream.Position = 0;

        await FollowupWithFileAsync(stream,
            fileName: "PlayerCard.png",
            text: $"[Profile Link](<https://beatleader.com/u/{player.PlayerLinkedAccounts.BeatLeaderId}>)");
    }
}