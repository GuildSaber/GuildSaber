using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.Server.Guilds.Members;
using GuildSaber.Database.Models.Server.Hardware;
using GuildSaber.Database.Models.Server.MapRequests;
using GuildSaber.Database.Models.Server.MapRequests.Proposals;
using GuildSaber.Database.Models.Server.MapRequests.Votes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Players;

public class Player
{
    public PlayerId Id { get; init; }
    public required PlayerInfo Info { get; set; }
    public required PlayerLinkedAccounts LinkedAccounts { get; set; }
    public required PlayerSubscriptionInfo SubscriptionInfo { get; set; }
    public required EPlatform Platform { get; set; }
    public HmdId? HmdId { get; set; }
    public bool IsManager { get; set; }

    public Hmd? Hmd { get; set; }
    public IList<Member> Members { get; init; } = null!;
    public IList<MapRequestThread> RequestThreads { get; init; } = null!;
    public IList<MapProposal> Proposals { get; init; } = null!;
    public IList<MapRequestThreadVote> RequestThreadVotes { get; init; } = null!;
    public IList<MapProposalVote> ProposalVotes { get; init; } = null!;

    public enum EPlatform
    {
        Unknown = 0,
        Steam = 1,
        MetaNative = 2,
        MetaPC = 3
    }
}

public class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(from => from.Value, to => new PlayerId(to))
            .ValueGeneratedOnAdd();

        builder.ComplexProperty(x => x.Info);
        builder.ComplexProperty(x => x.LinkedAccounts).Configure(new PlayerLinkedAccountsConfiguration());
        builder.ComplexProperty(x => x.SubscriptionInfo);

        builder.HasIndex(x => x.LinkedAccounts.SteamId);
        builder.HasIndex(x => x.LinkedAccounts.MetaPCId);
        builder.HasIndex(x => x.LinkedAccounts.BLNativeId);
        builder.HasIndex(x => x.LinkedAccounts.ScoreSaberId);
        builder.HasIndex(x => x.LinkedAccounts.DiscordId);

        builder.HasOne(x => x.Hmd)
            .WithMany().HasForeignKey(x => x.HmdId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Members)
            .WithOne(x => x.Player)
            .HasForeignKey(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.RequestThreads)
            .WithOne(x => x.Author)
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Proposals)
            .WithOne(x => x.Author)
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}