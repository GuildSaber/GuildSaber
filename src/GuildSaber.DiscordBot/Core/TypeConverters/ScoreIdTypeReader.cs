using Discord;
using Discord.Interactions;

namespace GuildSaber.DiscordBot.Core.TypeConverters;

public class ScoreIdTypeReader : TypeReader<ScoreId>
{
    public override Task<TypeConverterResult> ReadAsync(
        IInteractionContext context, string option, IServiceProvider services)
        => Task.FromResult(int.TryParse(option, out var scoreId)
            ? TypeConverterResult.FromSuccess(new ScoreId(scoreId))
            : TypeConverterResult.FromError(InteractionCommandError.ConvertFailed, "Invalid ScoreId format."));
}