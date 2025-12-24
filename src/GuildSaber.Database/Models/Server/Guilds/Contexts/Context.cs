using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.Server.Guilds.Levels;
using GuildSaber.Database.Models.Server.Guilds.Members;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.Server.RankedMaps;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds;

public class Context
{
    public ContextId Id { get; init; }
    public GuildId GuildId { get; init; }

    public EContextType Type { get; init; }

    public ContextInfo Info { get; set; }
    //TODO: Add settings for context, like if it only takes up new scores, etc.

    public IList<Point> Points { get; set; } = null!;
    public IList<Level> Levels { get; set; } = null!;
    public IList<RankedMap> RankedMaps { get; init; } = null!;
    public IList<Member> Members { get; set; } = null!;
    public IList<ContextMember> ContextMembers { get; init; } = null!;

    /// <summary>
    /// Maybe this will end up being a type union (from inheritance), but it will fit for now.
    /// </summary>
    public enum EContextType
    {
        Default = 0,
        Tournament = 1 << 0,
        Temporary = 1 << 1
    }
}

public class ContextConfiguration : IEntityTypeConfiguration<Context>
{
    public void Configure(EntityTypeBuilder<Context> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(from => from.Value, to => new ContextId(to))
            .ValueGeneratedOnAdd();
        builder.ComplexProperty(x => x.Info).Configure(new ContextInfoConfiguration());

        builder.HasOne<Guild>()
            .WithMany(x => x.Contexts).HasForeignKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Points)
            .WithMany();
        builder.HasMany(x => x.Levels)
            .WithOne(x => x.Context).HasForeignKey(x => x.ContextId);
        builder.HasMany(x => x.RankedMaps)
            .WithOne().HasForeignKey(x => x.ContextId);
        builder.HasMany(x => x.Members)
            .WithMany(x => x.Contexts)
            .UsingEntity<ContextMember>();
    }
}