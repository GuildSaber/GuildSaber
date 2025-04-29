using GuildSaber.Database.Utils;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Guilds;

public readonly record struct GuildJoinRequirements(
    GuildJoinRequirements.Requirements Flags,
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

public class GuildJoinRequirementsConfiguration : IComplexPropertyConfiguration<GuildJoinRequirements>
{
    public ComplexPropertyBuilder<GuildJoinRequirements> Configure(ComplexPropertyBuilder<GuildJoinRequirements> builder) 
        => throw new NotImplementedException();
}