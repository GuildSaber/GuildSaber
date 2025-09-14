using GuildSaber.Database.Models.Server.Guilds.Members;
using GuildSaber.Database.Models.Server.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds;

/// <remarks>
/// Class created to add future functionality such as join requirements (e.g. rank, invite code, etc.)
/// </remarks>
public class GuildContextMember
{
    public GuildContext.GuildContextId GuildContextId { get; init; }
    public Guild.GuildId GuildId { get; init; }
    public Player.PlayerId PlayerId { get; init; }
}

public class GuildContextMemberConfiguration : IEntityTypeConfiguration<GuildContextMember>
{
    public void Configure(EntityTypeBuilder<GuildContextMember> builder)
    {
        builder.HasKey(x => new { x.GuildContextId, x.GuildId, x.PlayerId });

        builder.HasOne<GuildContext>()
            .WithMany(x => x.ContextMembers);
        builder.HasOne<Member>()
            .WithMany(x => x.ContextMembers);
    }
}