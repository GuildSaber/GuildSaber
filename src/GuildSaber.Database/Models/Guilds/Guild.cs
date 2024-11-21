using GuildSaber.Database.Models.Guilds.Navigation;
using GuildSaber.Database.Models.StrongTypes.Abstractions;
using GuildSaber.Database.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Guilds;

public class Guild
{
    public GuildId Id { get; init; } = default;
    public GuildInfo Info { get; set; }
    public GuildJoinRequirements Requirements { get; set; }

    public IList<Member> Members { get; init; } = null!;
    public IList<GuildContext> Contexts { get; init; } = null!;
    
    public readonly record struct GuildId(ulong Value) : IStrongType<ulong>;
}

public class GuildConfiguration : IEntityTypeConfiguration<Guild>
{
    public void Configure(EntityTypeBuilder<Guild> builder)
    {
        builder.Property(x => x.Id).HasGenericConversion<Guild.GuildId, ulong>();
        builder.ComplexProperty(x => x.Info).Configure(new GuildInfoConfiguration());
        builder.ComplexProperty(x => x.Requirements);
    }
}