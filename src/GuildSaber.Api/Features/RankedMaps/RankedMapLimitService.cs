using CSharpFunctionalExtensions;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds.Boosts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GuildSaber.Api.Features.RankedMaps;

public sealed class RankedMapLimitService(ServerDbContext dbContext, IOptions<RankedMapSettings> rankedMapSettings)
{
    public readonly record struct RankedMapBoostsCount(int Tier1, int Tier2, int Tier3);

    public Task<int> GetCurrentGuildRankedMapCount(GuildId guildId)
        => dbContext.RankedMaps.CountAsync(x => x.GuildId == guildId);

    internal UnitResult<(int current, int max)> ValidateRankedMapCreationLimit(
        int currentCount, RankedMapBoostsCount boostsCount)
    {
        var settings = rankedMapSettings.Value;
        var max = settings.DefaultSettings.MaxRankedMapCount;

        max += boostsCount.Tier1 * settings.BoostSettings.MapCountBoosts.Tier1;
        max += boostsCount.Tier2 * settings.BoostSettings.MapCountBoosts.Tier2;
        max += boostsCount.Tier3 * settings.BoostSettings.MapCountBoosts.Tier3;

        return currentCount switch
        {
            var count when count >= max => Failure((count, max)),
            _ => UnitResult.Success<(int, int)>()
        };
    }

    public async Task<RankedMapBoostsCount> GetBoostsCountsAsync(GuildId guildId) => await dbContext
            .Boosts.Where(x => x.GuildId == guildId)
            .GroupBy(x => x.Type)
            .Select(x => new { Type = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count) switch
        {
            null => new RankedMapBoostsCount(0, 0, 0),
            var boostCounts => new RankedMapBoostsCount(
                Tier1: boostCounts.GetValueOrDefault(Boost.EBoostType.Tier1, 0),
                Tier2: boostCounts.GetValueOrDefault(Boost.EBoostType.Tier2, 0),
                Tier3: boostCounts.GetValueOrDefault(Boost.EBoostType.Tier3, 0)
            )
        };
}