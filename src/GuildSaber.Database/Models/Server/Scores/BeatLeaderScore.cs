using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Scores;

public sealed class BeatLeaderScore : AbstractScore
{
    public required BeatLeaderScoreId? BeatLeaderScoreId { get; init; }
}

public class BeatLeaderScoreConfiguration : IEntityTypeConfiguration<BeatLeaderScore>
{
    public void Configure(EntityTypeBuilder<BeatLeaderScore> builder)
    {
        builder.HasBaseType<AbstractScore>();
        builder.Property(x => x.BeatLeaderScoreId)
            .HasConversion<int?>(from => from, to => BeatLeaderScoreId.CreateUnsafe(to));
    }
}