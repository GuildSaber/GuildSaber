using GuildSaber.Common.Services.ScoreSaber.Models.StrongTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Scores;

public sealed class ScoreSaberScore : AbstractScore
{
    public required ScoreSaberScoreId ScoreSaberScoreId { get; init; }
    public required string? DeviceHmd { get; init; }
    public required string? DeviceControllerLeft { get; init; }
    public required string? DeviceControllerRight { get; init; }
}

public class ScoreSaberScoreConfiguration : IEntityTypeConfiguration<ScoreSaberScore>
{
    public void Configure(EntityTypeBuilder<ScoreSaberScore> builder)
    {
        builder.HasBaseType<AbstractScore>();

        builder.Property(x => x.ScoreSaberScoreId)
            .HasConversion<int>(from => from, to => ScoreSaberScoreId.CreateUnsafe(to).Value);
        builder.Property(x => x.DeviceHmd).HasMaxLength(20);
        builder.Property(x => x.DeviceControllerLeft).HasMaxLength(20);
        builder.Property(x => x.DeviceControllerRight).HasMaxLength(20);
    }
}