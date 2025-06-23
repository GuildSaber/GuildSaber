using GuildSaber.Database.Extensions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds;

public readonly record struct GuildRequirements(
    bool RequireSubmission,
    uint? MinRank,
    uint? MaxRank,
    uint? MinPP,
    uint? MaxPP,
    uint? AccountAgeUnix
);

public static class GuildRequirementsExtensions
{
    public abstract record GuildRequirement
    {
        public record RequireSubmission : GuildRequirement;
        public record MinRank(uint Value) : GuildRequirement;
        public record MaxRank(uint Value) : GuildRequirement;
        public record MinPP(uint Value) : GuildRequirement;
        public record MaxPP(uint Value) : GuildRequirement;
        public record AccountAgeUnix(uint Value) : GuildRequirement;
    }

    public static IEnumerable<GuildRequirement> Collect(this GuildRequirements requirements)
    {
        if (requirements.RequireSubmission)
            yield return new GuildRequirement.RequireSubmission();

        if (requirements.MinRank.HasValue)
            yield return new GuildRequirement.MinRank(requirements.MinRank.Value);

        if (requirements.MaxRank.HasValue)
            yield return new GuildRequirement.MaxRank(requirements.MaxRank.Value);

        if (requirements.MinPP.HasValue)
            yield return new GuildRequirement.MinPP(requirements.MinPP.Value);

        if (requirements.MaxPP.HasValue)
            yield return new GuildRequirement.MaxPP(requirements.MaxPP.Value);

        if (requirements.AccountAgeUnix.HasValue)
            yield return new GuildRequirement.AccountAgeUnix(requirements.AccountAgeUnix.Value);
    }
}

public class GuildJoinRequirementsConfiguration : IComplexPropertyConfiguration<GuildRequirements>
{
    public ComplexPropertyBuilder<GuildRequirements> Configure(ComplexPropertyBuilder<GuildRequirements> builder)
        => builder;
}