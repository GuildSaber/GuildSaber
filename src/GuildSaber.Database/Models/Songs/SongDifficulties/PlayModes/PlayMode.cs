using GuildSaber.Database.Models.StrongTypes.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Songs.SongDifficulties.PlayModes;

public class PlayMode
{
    public PlayModeId Id { get; init; }
    public required string Name { get; set; }
    
    public readonly record struct PlayModeId(ulong Value) : IStrongType<ulong>;
}

public class PlayModeConfiguration : IEntityTypeConfiguration<PlayMode>
{
    public void Configure(EntityTypeBuilder<PlayMode> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasGenericConversion<PlayMode.PlayModeId, ulong>();
        builder.Property(x => x.Name).HasMaxLength(128);
    }
}