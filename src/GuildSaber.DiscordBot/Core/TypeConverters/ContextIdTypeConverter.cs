using Discord;
using Discord.Interactions;

namespace GuildSaber.DiscordBot.Core.TypeConverters;

public class ContextIdComponentTypeConverter : ComponentTypeConverter<ContextId>
{
    public override Task<TypeConverterResult> ReadAsync(
        IInteractionContext context, IComponentInteractionData option, IServiceProvider services)
        => Task.FromResult(TypeConverterResult.FromSuccess(new ContextId(Convert.ToInt32(option.Value))));
}

public class ContextIdTypeConverter : TypeConverter<ContextId>
{
    public override ApplicationCommandOptionType GetDiscordType()
        => ApplicationCommandOptionType.Integer;

    public override Task<TypeConverterResult> ReadAsync(
        IInteractionContext context, IApplicationCommandInteractionDataOption option, IServiceProvider services)
        => Task.FromResult(TypeConverterResult.FromSuccess(new ContextId(Convert.ToInt32(option.Value))));
}