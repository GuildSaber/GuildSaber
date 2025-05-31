using GuildSaber.Database.Models.Server.Players;
using GuildSaber.Database.Models.StrongTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Auth;

/// <summary>
/// Represents a player session in the server.
/// <remarks>
/// The <see cref="IssuedAt" /> and <see cref="ExpiresAt" /> are identical to the JWT issued at and expiration times.
/// Therefore, if the JWT is valid, the session is also valid only if <see cref="IsValid" /> is true.
/// </remarks>
/// </summary>
public class Session
{
    public required UuidV7 SessionId { get; init; }
    public required Player.PlayerId PlayerId { get; init; }
    public required DateTimeOffset IssuedAt { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public required string Browser { get; init; }
    public required string BrowserVersion { get; init; }
    public required string Platform { get; init; }
    public required bool IsValid { get; init; }
}

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.HasKey(x => x.SessionId);
        builder.Property(x => x.SessionId)
            .HasConversion<Guid>(from => from, to => UuidV7.CreateUnsafe(to)!.Value)
            .ValueGeneratedNever()
            .IsRequired();
        builder.HasIndex(x => x.PlayerId);

        builder.Property(x => x.IssuedAt).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.Browser).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Platform).IsRequired().HasMaxLength(100);
        builder.Property(x => x.BrowserVersion).IsRequired().HasMaxLength(200);
        builder.Property(x => x.IsValid).IsRequired();

        builder.HasOne<Player>().WithMany().HasForeignKey(x => x.PlayerId);
    }
}