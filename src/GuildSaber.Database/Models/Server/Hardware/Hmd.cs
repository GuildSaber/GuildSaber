using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Hardware;

public class Hmd
{
    public HmdId Id { get; init; }
    public BeatLeaderHmdId? BeatLeaderHmdId { get; set; }
    public string? BeatLeaderKey { get; set; }
    public string? ScoreSaberKey { get; set; }
    public required string Name { get; set; }
}

public class HmdConfiguration : IEntityTypeConfiguration<Hmd>
{
    public void Configure(EntityTypeBuilder<Hmd> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(from => from.Value, to => new HmdId(to))
            .ValueGeneratedOnAdd();

        builder.Property(x => x.BeatLeaderHmdId)
            .HasConversion<int?>(from => from, to => BeatLeaderHmdId.CreateUnsafe(to));
        builder.HasIndex(x => x.BeatLeaderHmdId).IsUnique();

        builder.Property(x => x.BeatLeaderKey).HasMaxLength(64);
        builder.HasIndex(x => x.BeatLeaderKey).IsUnique();

        builder.Property(x => x.ScoreSaberKey).HasMaxLength(64);
        builder.HasIndex(x => x.ScoreSaberKey).IsUnique();

        builder.Property(x => x.Name).HasMaxLength(64);
    }
}