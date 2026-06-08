using System.ComponentModel;
using GuildSaber.Database.Models.StrongTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.RankedScores;

/// <summary>
/// An abstraction to represent a common <see cref="RawPoints" /> property for insight regarding how much point a ranked
/// score could give.
/// </summary>
public abstract class ScoredRankedScore : RankedScore
{
    public required RawPoints RawPoints { get; set; }
}

/// <summary> An abstraction to represent ranked scores that gives points to the player for easier querying.</summary>
/// <remarks>
/// You might wanna switch on the subtypes <see cref="ValidRankedScore" /> and <see cref="AcceptedRankedScore" />
/// </remarks>
public abstract class PointGivingRankedScore : ScoredRankedScore
{
    public required int Rank { get; set; }
}

public sealed class ValidRankedScore : PointGivingRankedScore;
public sealed class PendingRankedScore : ScoredRankedScore;
public sealed class AcceptedRankedScore : PointGivingRankedScore;
public sealed class RefusedRankedScore : ScoredRankedScore;

public sealed class InvalidRankedScore : RankedScore
{
    public required EInvalidReason InvalidReason { get; set; }

    [Flags]
    public enum EInvalidReason
    {
        [Description("No reason specified.")]
        Unspecified = 0,

        [Description("Score did not meet the minimum score requirement.")]
        MinAccuracyRequirements = 1 << 0,

        [Description("Score used prohibited modifiers.")]
        ProhibitedModifiers = 1 << 1,

        [Description("Score was missing required modifiers.")]
        MissingModifiers = 1 << 2,

        [Description("Score had too much pause time.")]
        PausedTooMuch = 1 << 3,

        [Description("Score was not a full combo when one was required.")]
        NoFullCombo = 1 << 4,

        [Description("Score was missing trackers.")]
        MissingTrackers = 1 << 5
    }
}

public class ScoredRankedScoreConfiguration : IEntityTypeConfiguration<ScoredRankedScore>
{
    public void Configure(EntityTypeBuilder<ScoredRankedScore> builder)
    {
        builder.HasBaseType<RankedScore>();

        builder.Property(x => x.RawPoints)
            .HasConversion<float>(from => from, to => RawPoints.CreateUnsafe(to).Value);
    }
}

public class PointGivingRankedScoreConfiguration : IEntityTypeConfiguration<PointGivingRankedScore>
{
    public void Configure(EntityTypeBuilder<PointGivingRankedScore> builder)
    {
        builder.HasBaseType<ScoredRankedScore>();

        builder.HasIndex(x => new { x.ContextId, x.PlayerId, x.RawPoints, x.Id })
            .IsDescending(false, false, true, false);
    }
}

public class ValidRankedScoreConfiguration : IEntityTypeConfiguration<ValidRankedScore>
{
    public void Configure(EntityTypeBuilder<ValidRankedScore> builder)
        => builder.HasBaseType<PointGivingRankedScore>();
}

public class PendingRankedScoreConfiguration : IEntityTypeConfiguration<PendingRankedScore>
{
    public void Configure(EntityTypeBuilder<PendingRankedScore> builder)
        => builder.HasBaseType<ScoredRankedScore>();
}

public class AcceptedRankedScoreConfiguration : IEntityTypeConfiguration<AcceptedRankedScore>
{
    public void Configure(EntityTypeBuilder<AcceptedRankedScore> builder)
        => builder.HasBaseType<PointGivingRankedScore>();
}

public class RefusedRankedScoreConfiguration : IEntityTypeConfiguration<RefusedRankedScore>
{
    public void Configure(EntityTypeBuilder<RefusedRankedScore> builder)
        => builder.HasBaseType<ScoredRankedScore>();
}

public class InvalidRankedScoreConfiguration : IEntityTypeConfiguration<InvalidRankedScore>
{
    public void Configure(EntityTypeBuilder<InvalidRankedScore> builder)
        => builder.HasBaseType<RankedScore>();
}