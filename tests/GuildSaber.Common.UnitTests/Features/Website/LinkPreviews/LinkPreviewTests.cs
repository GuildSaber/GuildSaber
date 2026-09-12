using System.Data.Common;
using System.Net;
using AwesomeAssertions;
using GuildSaber.Api.Features.Website.LinkPreviews;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;
using GuildSaber.Common.Settings;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.RankedMaps;
using GuildSaber.Database.Models.Server.Scores;
using GuildSaber.Database.Models.Server.Songs;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties;
using GuildSaber.Database.Models.StrongTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MyCSharp.HttpUserAgentParser.AspNetCore;
using MyCSharp.HttpUserAgentParser.AspNetCore.DependencyInjection;
using MyCSharp.HttpUserAgentParser.DependencyInjection;

namespace GuildSaber.Common.UnitTests.Features.Website.LinkPreviews;

public class LinkPreviewTests
{
    private sealed class StopBeforeOpeningConnectionInterceptor : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
            DbConnection connection,
            ConnectionEventData eventData,
            InterceptionResult result,
            CancellationToken cancellationToken = default)
            => throw new QueryTranslationSucceededException();
    }

    private sealed class QueryTranslationSucceededException : Exception;

    [Test]
    [Arguments("/maps/8872", 8872)]
    [Arguments("/maps/8872/", 8872)]
    [Arguments("/website/maps/8872", 8872)]
    [Arguments("/WEBSITE/MAPS/8872", 8872)]
    public void TryGetRankedMapId_ShouldRecognizeWebsiteMapRoutes(string path, long expectedId)
    {
        LinkPreviewMiddleware.TryGetRankedMapId(path, out var rankedMapId).Should().BeTrue();
        rankedMapId.Should().Be(new RankedMapId(expectedId));
    }

    [Test]
    [Arguments("/")]
    [Arguments("/maps")]
    [Arguments("/maps/not-an-id")]
    [Arguments("/maps/0")]
    [Arguments("/maps/-1")]
    [Arguments("/maps/8872/scores")]
    [Arguments("/website/players/8872")]
    public void TryGetRankedMapId_ShouldIgnoreOtherRoutes(string path)
        => LinkPreviewMiddleware.TryGetRankedMapId(path, out _).Should().BeFalse();

    [Test]
    [Arguments("Mozilla/5.0 (compatible; Discordbot/2.0; +https://discordapp.com)")]
    [Arguments("facebookexternalhit/1.1 (+http://www.facebook.com/externalhit_uatext.php)")]
    [Arguments("Slackbot-LinkExpanding 1.0 (+https://api.slack.com/robots)")]
    public async Task Middleware_ShouldRecognizeLinkPreviewCrawler(string userAgentValue)
    {
        ServiceCollection services = [];
        services.AddScoped<RankedMapLinkPreviewService>();
        services.AddHybridCache();
        services.AddHttpContextAccessor()
            .AddHttpUserAgentParser()
            .AddHttpUserAgentParserAccessor();

        await using var serviceProvider = services.BuildServiceProvider();

        var preview = CreateRankedMapLinkPreview();
        await serviceProvider.GetRequiredService<HybridCache>()
            .SetAsync($"RankedMapLinkPreview_{preview.Id}", preview);

        await using var scope = serviceProvider.CreateAsyncScope();

        var nextCalled = false;
        var middleware = new LinkPreviewMiddleware(_ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            serviceProvider.GetRequiredService<IHttpUserAgentParserAccessor>(),
            Options.Create(new LinkSettings
            {
                ApiBaseUri = new Uri("https://api.guildsaber.com"),
                WebsiteBaseUri = new Uri("https://guildsaber.com"),
                CdnBaseUri = new Uri("https://cdn.guildsaber.com")
            }));

        var context = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider,
            Request =
            {
                Method = HttpMethods.Get,
                Path = "/website/maps/8872",
                Headers =
                {
                    UserAgent = userAgentValue
                }
            }
        };

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        context.Response.Headers.CacheControl.ToString().Should().Be("public, max-age=3600");
    }

    [Test]
    public async Task Middleware_ShouldPassBrowserThroughWithoutResolvingDatabaseService()
    {
        ServiceCollection services = [];
        services.AddHttpContextAccessor()
            .AddHttpUserAgentParser()
            .AddHttpUserAgentParserAccessor();

        await using var serviceProvider = services.BuildServiceProvider();
        var nextCalled = false;
        var middleware = new LinkPreviewMiddleware(_ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            serviceProvider.GetRequiredService<IHttpUserAgentParserAccessor>(),
            Options.Create(new LinkSettings
            {
                ApiBaseUri = new Uri("https://api.guildsaber.com"),
                WebsiteBaseUri = new Uri("https://guildsaber.com"),
                CdnBaseUri = new Uri("https://cdn.guildsaber.com")
            }));

        var context = new DefaultHttpContext
        {
            RequestServices = serviceProvider,
            Request =
            {
                Method = HttpMethods.Get,
                Path = "/website/maps/8872",
                Headers =
                {
                    UserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 " +
                                "(KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36"
                }
            }
        };

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.Headers.Vary.ToString().Should().Contain("User-Agent");
    }

    [Test]
    public async Task RankedMapPreviewService_ShouldUseTranslatableDatabaseProjection()
    {
        var options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseNpgsql("Host=localhost;Port=1;Database=translation-test;Username=test;Password=test")
            .AddInterceptors(new StopBeforeOpeningConnectionInterceptor())
            .Options;

        ServiceCollection services = [];
        services.AddScoped(_ => new ServerDbContext(options));
        services.AddScoped<RankedMapLinkPreviewService>();
        services.AddHybridCache();

        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<RankedMapLinkPreviewService>();

        var exception = await Assert.ThrowsAsync<QueryTranslationSucceededException>(async () =>
            await service.GetAsync(new RankedMapId(8872), CancellationToken.None));

        exception.Should().NotBeNull();
    }

    [Test]
    public async Task RankedMapPreviewService_ShouldReturnHybridCachedPreviewWithoutQueryingDatabase()
    {
        var preview = CreateRankedMapLinkPreview();
        var options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseNpgsql("Host=localhost;Port=1;Database=cache-test;Username=test;Password=test")
            .AddInterceptors(new StopBeforeOpeningConnectionInterceptor())
            .Options;

        ServiceCollection services = [];
        services.AddScoped(_ => new ServerDbContext(options));
        services.AddScoped<RankedMapLinkPreviewService>();
        services.AddHybridCache();

        await using var serviceProvider = services.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<HybridCache>();

        await cache.SetAsync($"RankedMapLinkPreview_{preview.Id}", preview);
        await using var scope = serviceProvider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<RankedMapLinkPreviewService>();

        var result = await service.GetAsync(preview.Id, CancellationToken.None);
        result.Should().BeEquivalentTo(preview);
        ((float)result.DifficultyStats.NoteJumpSpeed).Should().Be(18f);
        ((int)result.DifficultyStats.MaxScore).Should().Be(100_000);
        ((float)result.Requirements.MinAccuracy!.Value).Should().Be(95.5f);
        result.Categories.Select(category => category.ToString())
            .Should().Equal("Challenge", "Tech & Flow");
    }

    [Test]
    public void Render_ShouldBuildDiscordFriendlyMapMetadataAndEscapeRemoteText()
    {
        var preview = CreateRankedMapLinkPreview();

        var html = RankedMapLinkPreviewRenderer.Render(
            preview,
            "https://dev.guildsaber.com/maps/8872?from=a&value=\"unsafe\"");
        var decodedHtml = WebUtility.HtmlDecode(html);

        html.Should().Contain("<meta property=\"og:site_name\" content=\"GuildSaber\">");
        html.Should().Contain("<meta name=\"twitter:card\" content=\"summary\">");
        html.Should().Contain("A &lt;dangerous&gt; &amp; ranked map");
        html.Should().Contain("Mapper &quot;Name&quot;");
        decodedHtml.Should().Contain("A <dangerous> & ranked map (!bsr abc12)");
        decodedHtml.Should().Contain("Difficulty: Expert+, One Saber");
        decodedHtml.Should().Contain("⭐: 9.50 | ✨: 4.25 (Acc > 95.5%)");
        decodedHtml.Should().Contain("NJS: 18 | NPS: 6.12 | Length: 02:05 | BPM: 180");
        decodedHtml.Should().Contain("Categories: Challenge, Tech & Flow");
        decodedHtml.Should().Contain("Prohibited Modifiers: NoBombs");
        decodedHtml.Should().Contain("Mandatory Modifiers: FasterSong");
        decodedHtml.Should().Contain("Requirements: Full Combo · Pause < 5s · Confirmation");
        html.Should().Contain("https://eu.cdn.beatsaver.com/1234567890abcdef1234567890abcdef12345678.jpg");
        html.Should().Contain("https://dev.guildsaber.com/maps/8872?from=a&amp;value=&quot;unsafe&quot;");
        html.Should().NotContain("<dangerous>");
        html.Should().NotContain("<script");
    }

    private static RankedMapLinkPreview CreateRankedMapLinkPreview() => new(
        new RankedMapId(8872),
        SongHash.CreateUnsafe("1234567890abcdef1234567890abcdef12345678")!.Value,
        BeatSaverKey.CreateUnsafe("abc12").Value,
        new SongInfo(
            BeatSaverName: "A <dangerous> & ranked map",
            SongName: "A dangerous song",
            SongSubName: string.Empty,
            SongAuthorName: "An artist",
            MapperName: "Mapper \"Name\""),
        new SongStats(BPM: 180f, DurationSec: 125f, IsAutoMapped: false),
        EDifficulty.ExpertPlus,
        "OneSaber",
        new SongDifficultyStats(
            MaxScore: MaxScore.CreateUnsafe(100_000).Value,
            NoteJumpSpeed: NJS.CreateUnsafe(18f).Value,
            NoteCount: 1_000,
            BombCount: 0,
            ObstacleCount: 0,
            NotesPerSecond: 6.123f,
            Duration: 125d),
        new RankedMapRating
        {
            DiffStar = new RankedMapRating.DifficultyStar(9.5f),
            AccStar = new RankedMapRating.AccuracyStar(4.25f)
        },
        [
            Name_2_50.CreateUnsafe("Challenge").Value,
            Name_2_50.CreateUnsafe("Tech & Flow").Value
        ],
        new RankedMapRequirements(
            NeedConfirmation: true,
            NeedFullCombo: true,
            MaxPauseDurationSec: 5,
            ProhibitedModifiers: AbstractScore.EModifiers.NoBombs,
            MandatoryModifiers: AbstractScore.EModifiers.FasterSong,
            MinAccuracy: Accuracy.CreateUnsafe(95.5f)));
}