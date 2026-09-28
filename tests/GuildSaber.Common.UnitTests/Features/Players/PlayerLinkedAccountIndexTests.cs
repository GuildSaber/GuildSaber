using AwesomeAssertions;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Players;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Common.UnitTests.Features.Players;

public class PlayerLinkedAccountIndexTests
{
    [Test]
    [Arguments(nameof(PlayerLinkedAccounts.SteamId))]
    [Arguments(nameof(PlayerLinkedAccounts.MetaPCId))]
    [Arguments(nameof(PlayerLinkedAccounts.BLNativeId))]
    [Arguments(nameof(PlayerLinkedAccounts.ScoreSaberId))]
    [Arguments(nameof(PlayerLinkedAccounts.DiscordId))]
    public void LinkedAccount_ShouldHaveNullableNonUniqueIndex(string propertyName)
    {
        using var db = new ServerDbContext(new DbContextOptionsBuilder<ServerDbContext>()
            .UseNpgsql("Host=localhost;Port=1;Database=model-test;Username=test;Password=test").Options);
        var player = db.Model.FindEntityType(typeof(Player))!;
        var property = player.FindComplexProperty(nameof(Player.LinkedAccounts))!.ComplexType
            .FindProperty(propertyName)!;

        property.IsNullable.Should().BeTrue();
        var index = player.GetIndexes().Single(x => x.Properties.SequenceEqual([property]));
        index.IsUnique.Should().BeFalse();

        // Verify provider DDL, not just metadata; generating the script does not connect to a database.
        db.Database.GenerateCreateScript().Should().Contain(
            $"CREATE INDEX \"IX_Players_LinkedAccounts_{propertyName}\" ON \"Players\" (\"LinkedAccounts_{propertyName}\")");
    }
}