using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Hardware;

public class Controller
{
    public ControllerId Id { get; init; }
    public BeatLeaderControllerId? BeatLeaderControllerId { get; set; }
    public string? BeatLeaderKey { get; set; }
    public string? ScoreSaberKey { get; set; }
    public required string Name { get; set; }
}

public class ControllerConfiguration : IEntityTypeConfiguration<Controller>
{
    public void Configure(EntityTypeBuilder<Controller> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(from => from.Value, to => new ControllerId(to))
            .ValueGeneratedOnAdd();

        builder.Property(x => x.BeatLeaderControllerId)
            .HasConversion<int?>(from => from, to => BeatLeaderControllerId.CreateUnsafe(to));
        builder.HasIndex(x => x.BeatLeaderControllerId).IsUnique();

        builder.Property(x => x.BeatLeaderKey).HasMaxLength(64);
        builder.HasIndex(x => x.BeatLeaderKey).IsUnique();

        builder.Property(x => x.ScoreSaberKey).HasMaxLength(64);
        builder.HasIndex(x => x.ScoreSaberKey).IsUnique();

        builder.Property(x => x.Name).HasMaxLength(64);
    }
}