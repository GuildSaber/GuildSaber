using GuildSaber.Database.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.RankedMaps.MapVersions.PlayModes;

public class PlayMode
{
    public PlayModeId Id { get; init; }
    public required string Name { get; set; }

    public readonly record struct PlayModeId(ulong Value) : IEFStrongTypedId<PlayModeId, ulong>
    {
        public static bool TryParse(string from, out PlayModeId value)
        {
            if (ulong.TryParse(from, out var id))
            {
                value = new PlayModeId(id);
                return true;
            }

            value = default;
            return false;
        }

        public static implicit operator ulong(PlayModeId id)
            => id.Value;

        public override string ToString()
            => Value.ToString();
    }
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