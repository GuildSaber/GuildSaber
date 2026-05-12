using Discord;
using Discord.Interactions;

namespace GuildSaber.DiscordBot.Core.TypeConverters;

public class CategoryIdTypeConverter : TypeConverter<CategoryId>
{
    public override ApplicationCommandOptionType GetDiscordType()
        => ApplicationCommandOptionType.Integer;

    public override Task<TypeConverterResult> ReadAsync(
        IInteractionContext context, IApplicationCommandInteractionDataOption option, IServiceProvider services)
        => Task.FromResult(TypeConverterResult.FromSuccess(new CategoryId(Convert.ToInt32(option.Value))));
}