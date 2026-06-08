using Discord;
using Discord.Interactions;
using GuildSaber.Api.Features.RankedScores.Http;

namespace GuildSaber.DiscordBot.Core.TypeConverters;

public sealed class ERankedScoreTypeConverter : TypeConverter<RankedScoreRequests.ERankedScoreType>
{
    public override ApplicationCommandOptionType GetDiscordType()
        => ApplicationCommandOptionType.Integer;

    public override Task<TypeConverterResult> ReadAsync(
        IInteractionContext context, IApplicationCommandInteractionDataOption option, IServiceProvider services)
        => Task.FromResult(TypeConverterResult.FromSuccess(option.Value is null
            ? RankedScoreRequests.ERankedScoreType.None
            : (RankedScoreRequests.ERankedScoreType)Convert.ToInt32(option.Value)));
}
