using GuildSaber.Database.Models.Guilds;
using GuildSaber.Database.Models.StrongTypes.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models;

public class GuildContext
{
    public readonly record struct GuildContextId(ulong Value) : IStrongType<ulong>;

    public GuildContextId Id { get; init; }
    public Guild.GuildId GuildId { get; init; }
    public ContextType Type { get; init; }
    public GuildContextInfo Info { get; set; }
    public GuildContextId? SyncedId { get; set; }

    public IList<GuildContext> Synced { get; init; } = null!;

    public enum ContextType
    {
        Default = 0,
        Tournament = 1,
        Temporary = 2
    }
}

public class GuildContextConfiguration : IEntityTypeConfiguration<GuildContext>
{
    public void Configure(EntityTypeBuilder<GuildContext> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasGenericConversion<GuildContext.GuildContextId, ulong>();
        builder.HasIndex(x => x.GuildId);
        builder.ComplexProperty(x => x.Info);

        builder.HasOne<GuildContext>().WithMany(x => x.Synced).HasForeignKey(x => x.SyncedId);
    }
}