using GuildSaber.Database.Models.StrongTypes.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Guild;

public class Guild
{
    public GuildId Id { get; init; } = default;
    public GuildInfo Info { get; set; }
    public GuildJoinRequirements Requirements { get; set; }
    
    public readonly record struct GuildId(ulong Value) : IStrongType<ulong>;
}

public class GuildConfiguration : IEntityTypeConfiguration<Guild>
{
    public void Configure(EntityTypeBuilder<Guild> builder)
    {
        builder.Property(x => x.Id).HasGenericConversion<Guild.GuildId, ulong>();
        builder.ComplexProperty(x => x.Info);
        builder.ComplexProperty(x => x.Requirements);
    }
}