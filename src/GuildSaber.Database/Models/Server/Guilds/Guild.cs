using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.Server.Guilds.Boosts;
using GuildSaber.Database.Models.Server.Guilds.Categories;
using GuildSaber.Database.Models.Server.Guilds.Members;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.Server.RankedMaps;
using GuildSaber.Database.Models.Server.RankedScores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds;

public class Guild
{
    public GuildId Id { get; init; }
    public required GuildInfo Info { get; set; }
    public required GuildRequirements Requirements { get; set; }
    public required EGuildStatus Status { get; init; }

    public IList<Context> Contexts { get; init; } = null!;
    public IList<Member> Members { get; init; } = null!;
    public IList<Boost> Boosts { get; init; } = null!;
    public IList<Point> Points { get; init; } = null!;
    public IList<RankedMap> RankedMaps { get; init; } = null!;
    public IList<RankedScore> RankedScores { get; init; } = null!;
    public IList<Category> Categories { get; init; } = null!;

    public enum EGuildStatus : byte
    {
        Unverified = 0,
        Verified = 1,
        Featured = 2,
        Private = 3
    }

    public readonly record struct GuildId(int Value) : IEFStrongTypedId<GuildId, int>
    {
        public static bool TryParse(string from, out GuildId value)
        {
            if (int.TryParse(from, out var id))
            {
                value = new GuildId(id);
                return true;
            }

            value = default;
            return false;
        }

        public static implicit operator int(GuildId id)
            => id.Value;

        public override string ToString()
            => Value.ToString();
    }
}

public class GuildConfiguration : IEntityTypeConfiguration<Guild>
{
    public void Configure(EntityTypeBuilder<Guild> builder)
    {
        builder.Property(x => x.Id)
            .HasGenericConversion<Guild.GuildId, int>()
            .ValueGeneratedOnAdd();
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

        builder.HasMany(x => x.RankedMaps)
            .WithOne().HasForeignKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.RankedScores)
            .WithOne().HasForeignKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Categories)
            .WithOne().HasForeignKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}