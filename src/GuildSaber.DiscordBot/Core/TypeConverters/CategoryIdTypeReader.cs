using Discord;
using Discord.Interactions;

namespace GuildSaber.DiscordBot.Core.TypeConverters;

public class CategoryIdTypeReader : TypeReader<CategoryId>
{
    public override Task<TypeConverterResult> ReadAsync(
        IInteractionContext context, string option, IServiceProvider services)
        => Task.FromResult(int.TryParse(option, out var CategoryId)
            ? TypeConverterResult.FromSuccess(new CategoryId(CategoryId))
            : TypeConverterResult.FromError(InteractionCommandError.ConvertFailed, "Invalid CategoryId format."));
}