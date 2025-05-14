using GuildSaber.Common.Result;
using GuildSaber.Database.Models.Server.StrongTypes;
using Xunit.Abstractions;

namespace GuildSaber.Database.UnitTests;

public class RankedMap { }

public class UnitTest1(ITestOutputHelper testOutputHelper)
{
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    [Fact]
    public async Task Test1() => await BLId.CreateAsync(123123u, new HttpClient())
        .Unwrap()
        .Match(
            some => _testOutputHelper.WriteLine(some.ToString()),
            () => _testOutputHelper.WriteLine("no")
        );
}