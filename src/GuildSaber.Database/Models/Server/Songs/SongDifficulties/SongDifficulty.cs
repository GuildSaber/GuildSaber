using GuildSaber.Database.Models.Server.Songs.SongDifficulties.GameModes;
using GuildSaber.Database.Models.Server.StrongTypes;
using GuildSaber.Database.Models.Server.StrongTypes.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Songs.SongDifficulties;

public class SongDifficulty
{
    public SongDifficultyId Id { get; init; }
    public BLLeaderboardId? BLLeaderboardId { get; init; }
    public GameMode.GameModeId GameModeId { get; init; }
    public Song.SongId SongId { get; init; }

    public GameMode GameMode { get; init; } = null!;
    public Song Song { get; init; } = null!;
    public readonly record struct SongDifficultyId(ulong Value) : IStrongType<ulong>;
}

public class SongDifficultyConfiguration : IEntityTypeConfiguration<SongDifficulty>
{
    public void Configure(EntityTypeBuilder<SongDifficulty> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasGenericConversion<SongDifficulty.SongDifficultyId, ulong>();
        builder.Property(x => x.BLLeaderboardId)
            .HasConversion<string?>(from => from, to => BLLeaderboardId.CreateUnsafe(to));

        builder.HasOne(x => x.GameMode).WithMany().HasForeignKey(x => x.GameModeId);
        builder.HasOne(x => x.Song).WithMany(x => x.SongDifficulties).HasForeignKey(x => x.SongId);
    }
}