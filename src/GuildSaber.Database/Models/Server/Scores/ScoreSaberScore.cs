using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Scores;

public sealed record ScoreSaberScore(
    string? DeviceHmd,
    string? DeviceControllerLeft,
    string? DeviceControllerRight
) : AbstractScore;

public class ScoreSaberScoreConfiguration : IEntityTypeConfiguration<ScoreSaberScore>
{
    public void Configure(EntityTypeBuilder<ScoreSaberScore> builder) => builder.HasBaseType<AbstractScore>();
}