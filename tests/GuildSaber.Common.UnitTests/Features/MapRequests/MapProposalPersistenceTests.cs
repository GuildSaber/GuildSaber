using AwesomeAssertions;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds.Achievements;
using GuildSaber.Database.Models.Server.Guilds.Categories;
using GuildSaber.Database.Models.Server.MapRequests;
using GuildSaber.Database.Models.Server.MapRequests.Proposals;
using GuildSaber.Database.Models.Server.MapRequests.Proposals.Snapshots;
using GuildSaber.Database.Models.Server.MapRequests.Proposals.Types;
using GuildSaber.Database.Models.Server.MapRequests.Votes;
using GuildSaber.Database.Models.Server.RankedMaps.MapVersions.PlayModes;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Common.UnitTests.Features.MapRequests;

public class MapProposalPersistenceTests
{
    [Test]
    [Arguments(typeof(MapRequestThread), nameof(ServerDbContext.MapRequestThreads))]
    [Arguments(typeof(MapRequestThreadBoost), nameof(ServerDbContext.MapRequestThreadBoosts))]
    [Arguments(typeof(MapProposal), nameof(ServerDbContext.MapProposals))]
    [Arguments(typeof(MapProposalVersion), nameof(ServerDbContext.MapProposalVersions))]
    [Arguments(typeof(MapProposalCategory), nameof(ServerDbContext.MapProposalCategories))]
    [Arguments(typeof(MapProposalAchievement), nameof(ServerDbContext.MapProposalAchievements))]
    [Arguments(typeof(MapProposalVote), nameof(ServerDbContext.MapProposalVotes))]
    public void TableName_ShouldFollowDbSetPropertyName(Type entityType, string tableName)
    {
        using var db = CreateContext();

        db.Model.FindEntityType(entityType)!.GetTableName().Should().Be(tableName);
    }

    [Test]
    [Arguments(typeof(MapProposalVersion), nameof(MapProposalVersion.PlayModeId), typeof(PlayMode))]
    [Arguments(typeof(MapProposalCategory), nameof(MapProposalCategory.CategoryId), typeof(Category))]
    [Arguments(typeof(MapProposalAchievement), nameof(MapProposalAchievement.AchievementId), typeof(Achievement))]
    public void HistoricalReference_ShouldPreserveIdentityWithoutDependingOnLiveEntity(
        Type snapshotType, string propertyName, Type liveType)
    {
        using var db = CreateContext();
        var snapshot = db.Model.FindEntityType(snapshotType)!;
        var identity = snapshot.FindProperty(propertyName)!;

        // Live-entity deletion must neither remove the snapshot nor clear its historical identity.
        snapshot.GetForeignKeys().Should().NotContain(x => x.PrincipalEntityType.ClrType == liveType);
        identity.IsNullable.Should().BeFalse();
        identity.GetTypeMapping().Converter!.ProviderClrType.Should().Be(typeof(int));

        var proposalLink = snapshot.GetForeignKeys()
            .Single(x => x.PrincipalEntityType.ClrType == typeof(MapProposal));
        proposalLink.IsRequired.Should().BeTrue();
        proposalLink.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
    }

    [Test]
    public void SongDifficulty_ShouldRemainRequiredAndProtectedFromDeletion()
    {
        using var db = CreateContext();
        var version = db.Model.FindEntityType(typeof(MapProposalVersion))!;
        var difficultyLink = version.GetForeignKeys()
            .Single(x => x.PrincipalEntityType.ClrType == typeof(SongDifficulty));

        difficultyLink.IsRequired.Should().BeTrue();
        difficultyLink.DeleteBehavior.Should().Be(DeleteBehavior.NoAction);
    }

    [Test]
    public void AcceptedProposal_WithCompleteConfiguration_ShouldTranslate()
    {
        using var db = CreateContext();
        var query = db.MapProposals.OfType<AcceptedProposal>()
            .Include(x => x.MapVersions).ThenInclude(x => x.SongDifficulty)
            .Include(x => x.Categories)
            .Include(x => x.Achievements)
            .AsSingleQuery();

        // Exercise the Npgsql mapping without opening a database connection.
        query.ToQueryString().Should().NotBeNullOrWhiteSpace();
        db.Database.GenerateCreateScript().Should().NotBeNullOrWhiteSpace();
    }

    private static ServerDbContext CreateContext()
        => new(new DbContextOptionsBuilder<ServerDbContext>()
            .UseNpgsql("Host=localhost;Port=1;Database=model-test;Username=test;Password=test").Options);
}