using GuildSaber.Database.Models.Songs.SongDifficulties;
using GuildSaber.Database.Models.StrongTypes;
using GuildSaber.Database.Models.StrongTypes.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Songs;

public class Song
{
    public SongId Id { get; init; }
    public SongHash Hash { get; init; }
    public BeatSaverKey? BeatSaverKey { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    public SongInfo Info { get; set; }
    public SongStats Stats { get; set; }

    public SongDifficulty[] SongDifficulties { get; init; } = null!;
    public readonly record struct SongId(ulong Value) : IStrongType<ulong>;
}

public class SongConfiguration : IEntityTypeConfiguration<Song>
{
    public void Configure(EntityTypeBuilder<Song> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Hash).IsUnique();
        builder.Property(x => x.Hash)
            .HasConversion<string>(from => from.ToString(), to => SongHash.CreateUnsafe(to).Value)
            .HasMaxLength(40);
        builder.Property(x => x.Id).HasGenericConversion<Song.SongId, ulong>();
        builder.Property(x => x.BeatSaverKey).HasConversion<string?>(from => from, to => BeatSaverKey.CreateUnsafe(to));
        builder.ComplexProperty(x => x.Info);
        builder.ComplexProperty(x => x.Stats);
    }
}