using GuildSaber.Database.Extensions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds;

public readonly record struct GuildRequirements(
    GuildRequirements.Requirements Flags,
    uint MinRank,
    uint MaxRank,
    uint MinPP,
    uint MaxPP,
    uint AccountAgeUnix)
{
    [Flags]
    public enum Requirements
    {
        None = 0,
        Submission = 1 << 0,
        MinRank = 1 << 1,
        MaxRank = 1 << 2,
        MinPP = 1 << 3,
        MaxPP = 1 << 4,
        AccountAgeUnix = 1 << 5
    }
}

public class GuildJoinRequirementsConfiguration : IComplexPropertyConfiguration<GuildRequirements>
{
    public ComplexPropertyBuilder<GuildRequirements> Configure(
        ComplexPropertyBuilder<GuildRequirements> builder)
        => builder;
}