using GuildSaber.Database.Models.Server.Guilds.Boosts;
using GuildSaber.Database.Models.Server.Guilds.Members;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.Server.StrongTypes.Abstractions;
using GuildSaber.Database.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds;

public class Guild
{
    public GuildId Id { get; init; }
    public GuildInfo Info { get; set; }
    public GuildJoinRequirements Requirements { get; set; }

    public IList<GuildContext> Contexts { get; init; } = null!;
    public IList<Member> Members { get; init; } = null!;
    public IList<Boost> Boosts { get; init; } = null!;
    public IList<Point> Points { get; init; } = null!;

    public readonly record struct GuildId(ulong Value) : IStrongType<ulong>
    {
        public static bool TryParse(string from, IFormatProvider formatProvider, out GuildId value)
            => throw new NotImplementedException();
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