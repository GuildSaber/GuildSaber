using Discord;
using Discord.Interactions;

namespace GuildSaber.DiscordBot.Core.TypeConverters;

public class MapRequestThreadIdTypeReader : TypeReader<MapRequestThreadId>
{
    public override Task<TypeConverterResult> ReadAsync(
        IInteractionContext context, string option, IServiceProvider services)
        => Task.FromResult(MapRequestThreadId.TryParse(option, out var id)
            ? TypeConverterResult.FromSuccess(id)
            : TypeConverterResult.FromError(InteractionCommandError.ConvertFailed, "Invalid thread ID."));
}

public class MapProposalIdTypeReader : TypeReader<MapProposalId>
{
    public override Task<TypeConverterResult> ReadAsync(
        IInteractionContext context, string option, IServiceProvider services)
        => Task.FromResult(MapProposalId.TryParse(option, out var id)
            ? TypeConverterResult.FromSuccess(id)
            : TypeConverterResult.FromError(InteractionCommandError.ConvertFailed, "Invalid proposal ID."));
}