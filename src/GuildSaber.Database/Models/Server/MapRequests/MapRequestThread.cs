using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.MapRequests.Proposals;
using GuildSaber.Database.Models.Server.Players;
using GuildSaber.Database.Models.Server.RankedMaps;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.MapRequests;

/// <summary>
/// A persistent container for a ranked map's proposals, including its initial ranking and later changes.
/// Submission, acceptance, and closure belong to individual proposals, not the thread.
/// </summary>
public sealed class MapRequestThread
{
    public MapRequestThreadId Id { get; init; }

    public ContextId ContextId { get; init; }
    public PlayerId? AuthorId { get; init; }

    /// <summary>Limits legacy synchronization to its own maps. Player-created rankings must never be swept by it.</summary>
    public bool IsLegacyImport { get; init; }

    // Could argue we could save the author name in case they get deleted but that would mean keeping data after profile deletion so I would rather not for something like that yet.
    public DateTimeOffset CreatedAt { get; init; }

    public Player? Author { get; init; }
    public RankedMap? RankedMap { get; init; }
    public IList<MapProposal> Proposals { get; init; } = null!;
    public IList<MapRequestThreadBoost> Boosts { get; init; } = null!;
}

public class MapRequestThreadConfiguration : IEntityTypeConfiguration<MapRequestThread>
{
    public void Configure(EntityTypeBuilder<MapRequestThread> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(from => from.Value, to => new MapRequestThreadId(to))
            .ValueGeneratedOnAdd();

        builder.HasOne<Context>()
            .WithMany(x => x.RequestThreads)
            .HasForeignKey(x => x.ContextId);

        builder.HasOne(x => x.Author)
            .WithMany(x => x.RequestThreads)
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}