using Discord;
using Discord.Interactions;

namespace GuildSaber.DiscordBot.Core.TypeConverters;

public class PlayerIdTypeReader : TypeReader<PlayerId>
{
    public override Task<TypeConverterResult> ReadAsync(IInteractionContext context, string option,
                                                        IServiceProvider services)
        => Task.FromResult(PlayerId.TryParse(option, out var playerId)
            ? TypeConverterResult.FromSuccess(playerId)
            : TypeConverterResult.FromError(InteractionCommandError.ConvertFailed, "Invalid PlayerId format."));
}