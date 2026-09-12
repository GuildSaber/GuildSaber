using AwesomeAssertions;
using GuildSaber.Api.Features.Guilds.Achievements.Playlists.Http;
using GuildSaber.Api.Features.RankedMaps.Http;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Database.Contexts.Server;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Common.UnitTests.Features.RankedMaps;

public class RankedMapMapperTests
{
    [Test]
    public void DetailedRankedMapProjection_ShouldTranslate()
    {
        var options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseNpgsql("Host=localhost;Port=1;Database=translation-test;Username=test;Password=test")
            .Options;

        using var dbContext = new ServerDbContext(options);
        var query = dbContext.RankedMaps
            .AsExpandable()
            .Select(RankedMapMappers.MapRankedMapExpression());

        var sql = query.ToQueryString();

        sql.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public void AchievementPlaylistProjections_ShouldTranslate()
    {
        var options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseNpgsql("Host=localhost;Port=1;Database=translation-test;Username=test;Password=test")
            .Options;

        using var dbContext = new ServerDbContext(options);
        var playerId = new PlayerId(1);
        var queries = new[]
        {
            dbContext.Achievements.AsExpandable()
                .Select(PlaylistMappers.MapPlaylistExpression(dbContext, null, null)),
            dbContext.Achievements.AsExpandable()
                .Select(PlaylistMappers.MapPlaylistPassedScoreExpression(dbContext, playerId, null, null)),
            dbContext.Achievements.AsExpandable()
                .Select(PlaylistMappers.MapPlaylistPassedOrPendingScoreExpression(dbContext, playerId, null, null))
        };

        foreach (var query in queries)
            query.ToQueryString().Should().NotBeNullOrWhiteSpace();
    }
}