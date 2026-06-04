using Discord;
using Discord.Interactions;
using GuildSaber.Api.Features.RankedScores.Http;

namespace GuildSaber.DiscordBot.Core.TypeConverters;

public class EStateTypeConverter : TypeConverter<RankedScoreResponses.EState>
{
    public override ApplicationCommandOptionType GetDiscordType()
        => ApplicationCommandOptionType.Integer;

    public override Task<TypeConverterResult> ReadAsync(
        IInteractionContext context, IApplicationCommandInteractionDataOption option, IServiceProvider services)
        => Task.FromResult(TypeConverterResult.FromSuccess(option.Value is null
            ? null
            : (RankedScoreResponses.EState?)Convert.ToInt32(option.Value)));
}