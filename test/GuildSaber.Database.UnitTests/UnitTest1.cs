using CSharpFunctionalExtensions;
using GuildSaber.Core.Result;
using GuildSaber.Database.Models.StrongTypes;
using Xunit.Abstractions;

namespace GuildSaber.Database.UnitTests;

public class RankedMap { }

public class UnitTest1(ITestOutputHelper testOutputHelper)
{
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    [Fact]
    public async Task Test1()
    {
        var bLId = await BLId.CreateAsync(123123u, new HttpClient());
        bLId.Match(
            value => _testOutputHelper.WriteLine(value.ToString()),
            error => _testOutputHelper.WriteLine(error)
        );
    }
}