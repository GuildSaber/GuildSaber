using Discord.Interactions;
using GuildSaber.DiscordBot.Core.Extensions;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [SlashCommand("importadminconf", "Import all ranked score admin conf states from the old guildsaber bot")]
    public async Task ImportAdminConf()
    {
        await DeferAsync();

        _ = await GetPlayerId(Context.User.DiscordId);
        var result = await Client.Value.Debug.ImportAdminConfStatesAtMeAsync();

        if (!result.TryGetValue(out var success, out var error))
        {
            await RespondAsync($"Failed to request import of admin conf states: {error}");
            return;
        }

        if (success) await FollowupAsync("Successfully requested the server to import ranked score admin conf states.");
        else await FollowupAsync("Player not found on legacy guildsaber system.");
    }
}