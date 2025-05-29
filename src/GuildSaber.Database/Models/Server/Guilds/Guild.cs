using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.Server.Guilds.Boosts;
using GuildSaber.Database.Models.Server.Guilds.Members;
using GuildSaber.Database.Models.Server.Guilds.Points;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds;

public class Guild
{
    public required GuildId Id { get; init; }
    public required GuildInfo Info { get; set; }
    public required GuildJoinRequirements Requirements { get; set; }

    public IList<GuildContext> Contexts { get; init; } = null!;
    public IList<Member> Members { get; init; } = null!;
    public IList<Boost> Boosts { get; init; } = null!;
    public IList<Point> Points { get; init; } = null!;

    public readonly record struct GuildId(ulong Value) : IEFStrongTypedId<GuildId, ulong>
    {
        public static bool TryParse(string from, out GuildId value)
        {
            if (ulong.TryParse(from, out var id))
            {
                value = new GuildId(id);
                return true;
            }

            value = default;
            return false;
        }

        public static implicit operator ulong(GuildId id)
            => id.Value;
    }
}

public class GuildConfiguration : IEntityTypeConfiguration<Guild>
{
    public void Configure(EntityTypeBuilder<Guild> builder)
    {
        builder.Property(x => x.Id).HasGenericConversion<Guild.GuildId, ulong>();
        builder.ComplexProperty(x => x.Info).Configure(new GuildInfoConfiguration());
        builder.ComplexProperty(x => x.Requirements).Configure(new GuildJoinRequirementsConfiguration());

        builder.HasMany(x => x.Contexts)
            .WithOne().HasForeignKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Members)
            .WithOne().HasForeignKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Boosts)
            .WithOne().HasForeignKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Points)
            .WithOne().HasForeignKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}