using Discord;
using Discord.Interactions;

namespace GuildSaber.DiscordBot.Core.TypeConverters;

public class ScoreIdComponentTypeConverter : ComponentTypeConverter<ScoreId>
{
    public override Task<TypeConverterResult> ReadAsync(
        IInteractionContext context, IComponentInteractionData option, IServiceProvider services)
        => Task.FromResult(TypeConverterResult.FromSuccess(new ScoreId(Convert.ToInt32(option.Value))));
}

public class ScoreIdTypeConverter : TypeConverter<ScoreId>
{
    public override ApplicationCommandOptionType GetDiscordType()
        => ApplicationCommandOptionType.Integer;

    public override Task<TypeConverterResult> ReadAsync(
        IInteractionContext context, IApplicationCommandInteractionDataOption option, IServiceProvider services)
        => Task.FromResult(TypeConverterResult.FromSuccess(new ScoreId(Convert.ToInt32(option.Value))));
}