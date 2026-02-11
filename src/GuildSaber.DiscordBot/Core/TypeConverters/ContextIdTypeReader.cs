using Discord;
using Discord.Interactions;

namespace GuildSaber.DiscordBot.Core.TypeConverters;

public class ContextIdTypeReader : TypeReader<ContextId>
{
    public override Task<TypeConverterResult> ReadAsync(
        IInteractionContext context, string option, IServiceProvider services)
        => Task.FromResult(int.TryParse(option, out var contextId)
            ? TypeConverterResult.FromSuccess(new ContextId(contextId))
            : TypeConverterResult.FromError(InteractionCommandError.ConvertFailed, "Invalid ContextId format."));
}