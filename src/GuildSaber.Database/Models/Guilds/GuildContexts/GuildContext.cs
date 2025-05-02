using GuildSaber.Database.Models.StrongTypes.Abstractions;
using GuildSaber.Database.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Guilds;

public class GuildContext
{
    public enum ContextType
    {
        Default = 0,
        Tournament = 1 << 0,
        Temporary = 1 << 1
    }

    public GuildContextId Id { get; init; }
    public Guild.GuildId GuildId { get; init; }

    public ContextType Type { get; init; }
    public GuildContextInfo Info { get; set; }
    public readonly record struct GuildContextId(ulong Value) : IStrongType<ulong>;
}

public class GuildContextConfiguration : IEntityTypeConfiguration<GuildContext>
{
    public void Configure(EntityTypeBuilder<GuildContext> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasGenericConversion<GuildContext.GuildContextId, ulong>();
        builder.HasIndex(x => x.GuildId);
        builder.ComplexProperty(x => x.Info).Configure(new GuildContextInfoConfiguration());
    }
}