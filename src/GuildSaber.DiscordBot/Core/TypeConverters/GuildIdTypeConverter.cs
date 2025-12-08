using Discord;
using Discord.Interactions;

namespace GuildSaber.DiscordBot.Core.TypeConverters;

public class GuildIdTypeConverter : TypeConverter<GuildId>
{
    public override ApplicationCommandOptionType GetDiscordType()
        => ApplicationCommandOptionType.Integer;

    public override Task<TypeConverterResult> ReadAsync(
        IInteractionContext context, IApplicationCommandInteractionDataOption option, IServiceProvider services)
        => Task.FromResult(TypeConverterResult.FromSuccess(new GuildId(Convert.ToInt32(option.Value))));
}