using GuildSaber.Database.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.RankedMaps.MapVersions.PlayModes;

public class PlayMode
{
    public PlayModeId Id { get; init; }
    public required string Name { get; set; }

    public readonly record struct PlayModeId(int Value) : IEFStrongTypedId<PlayModeId, int>
    {
        public static bool TryParse(string from, out PlayModeId value)
        {
            if (int.TryParse(from, out var id))
            {
                value = new PlayModeId(id);
                return true;
            }

            value = default;
            return false;
        }

        public static implicit operator int(PlayModeId id)
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
        builder.Property(x => x.Id)
            .HasGenericConversion<PlayMode.PlayModeId, int>()
            .ValueGeneratedOnAdd();
        builder.Property(x => x.Name).HasMaxLength(128);
    }
}