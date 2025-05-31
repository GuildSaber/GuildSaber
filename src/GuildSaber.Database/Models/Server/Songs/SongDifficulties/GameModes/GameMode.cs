using GuildSaber.Database.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Songs.SongDifficulties.GameModes;

public class GameMode
{
    public GameModeId Id { get; init; }
    public required string Name { get; set; }

    public readonly record struct GameModeId(ulong Value) : IEFStrongTypedId<GameModeId, ulong>
    {
        public static bool TryParse(string from, out GameModeId value)
        {
            if (ulong.TryParse(from, out var id))
            {
                value = new GameModeId(id);
                return true;
            }

            value = default;
            return false;
        }

        public static implicit operator ulong(GameModeId id)
            => id.Value;

        public override string ToString()
            => Value.ToString();
    }
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