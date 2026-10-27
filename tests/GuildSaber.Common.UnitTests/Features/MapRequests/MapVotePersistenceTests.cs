using AwesomeAssertions;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.MapRequests;
using GuildSaber.Database.Models.Server.MapRequests.Proposals;
using GuildSaber.Database.Models.Server.MapRequests.Proposals.Types;
using GuildSaber.Database.Models.Server.MapRequests.Votes;
using GuildSaber.Database.Models.Server.Players;
using GuildSaber.Database.Models.Server.RankedMaps;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Common.UnitTests.Features.MapRequests;

public class MapVotePersistenceTests
{
    [Test]
    public void Vote_ShouldHaveUniqueIdentityAndCascadeFromBothParents()
    {
        using var db = CreateContext();
        var vote = db.Model.FindEntityType(typeof(MapProposalVote))!;

        vote.FindPrimaryKey()!.Properties.Select(x => x.Name).Should().Equal("ProposalId", "PlayerId", "Kind");
        vote.FindProperty(nameof(MapProposalVote.ProposalId))!.GetTypeMapping().Converter!.ProviderClrType.Should()
            .Be(typeof(long));
        vote.FindProperty("PlayerId")!.GetTypeMapping().Converter!.ProviderClrType.Should().Be(typeof(int));

        var relationships = vote.GetForeignKeys().ToArray();
        relationships.Select(x => x.PrincipalEntityType.ClrType)
            .Should().BeEquivalentTo([typeof(MapProposal), typeof(Player)]);

        // Verify that unloaded votes also get deleted by the database, not only by EF's change tracker.
        var sql = db.Database.GenerateCreateScript();
        foreach (var relationship in relationships)
        {
            relationship.IsRequired.Should().BeTrue();
            relationship.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
            sql.Should().Contain(
                $"FOREIGN KEY (\"{relationship.Properties.Single().Name}\") " +
                $"REFERENCES \"{relationship.PrincipalEntityType.GetTableName()}\" (\"Id\") ON DELETE CASCADE");
        }
    }

    [Test]
    public void DeletingPlayer_ShouldDeleteBothProposalVoteKindsWithoutDeletingAcceptedProposal()
    {
        using var db = CreateContext();
        var timestamp = new DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);
        var player = new Player
        {
            Id = new PlayerId(1),
            Info = new PlayerInfo("Voter", "", "", timestamp),
            Platform = default,
            LinkedAccounts = default,
            SubscriptionInfo = default
        };
        var thread = new MapRequestThread
        {
            Id = new MapRequestThreadId(10),
            ContextId = new ContextId(1),
            CreatedAt = timestamp
        };
        var proposal = new AcceptedProposal
        {
            Id = new MapProposalId(20),
            ThreadId = thread.Id,
            Thread = thread,
            CreatedAt = timestamp,
            SubmittedAt = timestamp,
            AcceptedAt = timestamp,
            Requirements = new RankedMapRequirements(false, false, null, 0, 0, null),
            Rating = new MapProposalRating
                { AccStar = new RankedMapRating.AccuracyStar(1), DiffStar = new RankedMapRating.DifficultyStar(1) }
        };
        var communityVote = new CommunityProposalVote
        {
            ProposalId = proposal.Id,
            Proposal = proposal,
            PlayerId = player.Id,
            Player = player,
            IsPositive = true,
            VotedAt = timestamp
        };
        var rankingTeamVote = new RankingTeamProposalVote
        {
            ProposalId = proposal.Id,
            Proposal = proposal,
            PlayerId = player.Id,
            Player = player,
            IsPositive = true,
            VotedAt = timestamp
        };
        db.AttachRange(communityVote, rankingTeamVote);

        db.Remove(player);
        db.ChangeTracker.CascadeChanges();

        db.Entry(communityVote).State.Should().Be(EntityState.Deleted);
        db.Entry(rankingTeamVote).State.Should().Be(EntityState.Deleted);
        db.Entry(thread).State.Should().Be(EntityState.Unchanged);
        db.Entry(proposal).State.Should().Be(EntityState.Unchanged);
        proposal.AcceptedAt.Should().Be(timestamp);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public void ProposalVotes_ShouldAllowBothTypesButNotDuplicateSameType(bool duplicateRankingTeamVote)
    {
        using var db = CreateContext();
        var timestamp = new DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

        MapProposalVote CreateVote(bool rankingTeam) => rankingTeam
            ? new RankingTeamProposalVote
            {
                ProposalId = new MapProposalId(20),
                PlayerId = new PlayerId(1),
                IsPositive = false,
                VotedAt = timestamp,
                Reason = "The proposed difficulty rating needs adjustment."
            }
            : new CommunityProposalVote
            {
                ProposalId = new MapProposalId(20),
                PlayerId = new PlayerId(1),
                IsPositive = true,
                VotedAt = timestamp
            };

        db.AttachRange(CreateVote(false), CreateVote(true));
        db.ChangeTracker.Entries<MapProposalVote>().Should().HaveCount(2);

        Action attachDuplicate = () => db.Attach(CreateVote(duplicateRankingTeamVote));
        attachDuplicate.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void ProposalVoteHierarchy_ShouldMapReasonOnlyOnRankingTeamVariant()
    {
        using var db = CreateContext();
        var vote = db.Model.FindEntityType(typeof(MapProposalVote))!;
        var community = db.Model.FindEntityType(typeof(CommunityProposalVote))!;
        var rankingTeam = db.Model.FindEntityType(typeof(RankingTeamProposalVote))!;

        vote.FindDiscriminatorProperty()!.IsShadowProperty().Should().BeTrue();
        community.GetDiscriminatorValue().Should().Be((byte)0);
        rankingTeam.GetDiscriminatorValue().Should().Be((byte)1);
        community.GetTableName().Should().Be(vote.GetTableName());
        rankingTeam.GetTableName().Should().Be(vote.GetTableName());

        vote.FindProperty(nameof(RankingTeamProposalVote.Reason)).Should().BeNull();
        community.FindProperty(nameof(RankingTeamProposalVote.Reason)).Should().BeNull();
        rankingTeam.FindProperty(nameof(RankingTeamProposalVote.Reason))!.IsNullable.Should().BeTrue();

        db.Database.GenerateCreateScript().Should().Contain(
            "CHECK ((\"Kind\" = 0 AND \"Reason\" IS NULL) OR \"Kind\" = 1)");
    }

    [Test]
    public void SeparateCommunityAndRankingTeamVoteCounts_ShouldTranslate()
    {
        using var db = CreateContext();
        var proposals = db.MapProposals.OfType<SubmittedProposal>().Select(proposal => new
        {
            proposal.Id,
            CommunityPositiveVotes = proposal.Votes.OfType<CommunityProposalVote>().Count(vote => vote.IsPositive),
            CommunityNegativeVotes = proposal.Votes.OfType<CommunityProposalVote>().Count(vote => !vote.IsPositive),
            RankingTeamPositiveVotes = proposal.Votes.OfType<RankingTeamProposalVote>().Count(vote => vote.IsPositive),
            RankingTeamNegativeVotes = proposal.Votes.OfType<RankingTeamProposalVote>().Count(vote => !vote.IsPositive)
        });

        proposals.ToQueryString().Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public void RankingTeamVote_WithReason_ShouldTrackAndTranslateForEitherDirection(bool isPositive)
    {
        using var db = CreateContext();
        var reason = isPositive ? "The proposed rating matches the map." : "The proposed rating is too low.";
        var vote = new RankingTeamProposalVote
        {
            ProposalId = new MapProposalId(20),
            PlayerId = new PlayerId(1),
            IsPositive = isPositive,
            VotedAt = new DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeSpan.Zero),
            Reason = reason
        };
        db.Attach(vote);

        db.Entry(vote).Property(x => x.Reason).CurrentValue.Should().Be(reason);
        db.Entry(vote).Property<byte>("Kind").CurrentValue.Should().Be(1);

        var reviews = db.MapProposalVotes.OfType<RankingTeamProposalVote>()
            .Where(x => x.IsPositive == isPositive)
            .Select(x => new { x.PlayerId, x.IsPositive, x.Reason });
        reviews.ToQueryString().Should().Contain("\"Reason\"");
    }

    private static ServerDbContext CreateContext()
        => new(new DbContextOptionsBuilder<ServerDbContext>()
            .UseNpgsql("Host=localhost;Port=1;Database=model-test;Username=test;Password=test").Options);
}