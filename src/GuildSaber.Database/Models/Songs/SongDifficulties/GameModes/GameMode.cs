using GuildSaber.Database.Models.StrongTypes.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Songs.SongDifficulties.GameModes;

public class GameMode
{
    public GameModeId Id { get; init; }
    public string Name { get; set; } = null!;
    
    public readonly record struct GameModeId(ulong Value) : IStrongType<ulong>;
}

public class GameModeConfiguration : IEntityTypeConfiguration<GameMode>
{
    public void Configure(EntityTypeBuilder<GameMode> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasGenericConversion<GameMode.GameModeId, ulong>();
        builder.Property(x => x.Name).HasMaxLength(128);
    }
}