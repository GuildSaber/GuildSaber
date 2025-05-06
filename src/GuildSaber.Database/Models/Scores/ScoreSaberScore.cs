using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Scores;

public sealed record ScoreSaberScore : AbstractScore;

public class ScoreSaberScoreConfiguration : IEntityTypeConfiguration<ScoreSaberScore>
{
    public void Configure(EntityTypeBuilder<ScoreSaberScore> builder) => builder.HasBaseType<AbstractScore>();
}