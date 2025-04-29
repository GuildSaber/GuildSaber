using GuildSaber.Database.Models.Guilds.Boosts;
using GuildSaber.Database.Models.Guilds.Members;
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
    public IList<Boost> Boosts { get; init; } = null!;
    public IList<GuildContext> Contexts { get; init; } = null!;

    public readonly record struct GuildId(ulong Value) : IStrongType<ulong>
    {
        public static bool TryParse(string from, IFormatProvider formatProvider, out GuildId value) => throw new NotImplementedException();
    }
}

public class GuildConfiguration : IEntityTypeConfiguration<Guild>
{
    public void Configure(EntityTypeBuilder<Guild> builder)
    {
        builder.Property(x => x.Id).HasGenericConversion<Guild.GuildId, ulong>();
        builder.ComplexProperty(x => x.Info).Configure(new GuildInfoConfiguration());
        builder.ComplexProperty(x => x.Requirements).Configure(new GuildJoinRequirementsConfiguration());
        builder.OwnsMany(x => x.Members);
        builder.OwnsMany(x => x.Boosts);
        builder.OwnsMany(x => x.Contexts);
    }
}