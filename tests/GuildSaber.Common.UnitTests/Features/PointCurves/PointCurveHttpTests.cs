extern alias Client;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using AwesomeAssertions;
using GuildSaber.Api.Features.Auth;
using GuildSaber.Api.Features.Auth.Authorization;
using GuildSaber.Api.Features.Auth.CustomApiKey;
using GuildSaber.Api.Features.Auth.Sessions;
using GuildSaber.Api.Features.PointCurves;
using GuildSaber.Api.Features.PointCurves.Http;
using GuildSaber.Api.Queuing;
using GuildSaber.Api.Setup;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Members;
using GuildSaber.Database.Models.Server.Guilds.Points;
using LinqKit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ClientRequests = Client::GuildSaber.Api.Features.PointCurves.Http.PointCurveRequests;
using ClientPolicy = Client::GuildSaber.Api.Features.PointCurves.Http.PointCurveRequests.Policy;
using Context = GuildSaber.Database.Models.Server.Guilds.Context;

using JetBrains.Annotations;

using Sample = Client::GuildSaber.Api.Features.PointCurves.Http.PointCurveRequests.Sample;

namespace GuildSaber.Common.UnitTests.Features.PointCurves;

[PublicAPI]
public class PointCurveHttpTests
{
    private sealed class TestAuthentication(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            => Task.FromResult(Scheme.Name == SessionCookieDefaults.AuthenticationScheme
                               && Request.Headers["X-Test-Player"].FirstOrDefault() is { } id
                ? AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(AuthConstants.PlayerIdClaimType, id)], Scheme.Name)), Scheme.Name))
                : AuthenticateResult.NoResult());
    }

    private sealed class RecordingQueue : IHeavyBackgroundTaskQueue
    {
        public int Count { get; private set; }

        public ValueTask QueueBackgroundWorkItemAsync(
            Func<CancellationToken, ValueTask> workItem, CancellationToken token = default)
        {
            Count++;

            return ValueTask.CompletedTask;
        }

        public ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken token)
            => throw new NotSupportedException("These HTTP tests inspect queued work without running it.");
    }

    private sealed class Host(WebApplication app, RecordingQueue queue) : IAsyncDisposable
    {
        public RecordingQueue Queue { get; } = queue;

        public HttpClient CreateClient() => new() { BaseAddress = new Uri(app.Urls.Single()) };

        public static async Task<Host> StartAsync(
            PointCurveTestDatabase database,
            Member.EPermission permission = Member.EPermission.RankingTeam)
        {
            await database.Context.Members
                .Where(x => x.PlayerId == new PlayerId(1))
                .ExecuteUpdateAsync(x => x.SetProperty(m => m.Permissions, permission));

            var queue = new RecordingQueue();
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });

            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Logging.ClearProviders();
            builder.Logging
                .AddConsole()
                .SetMinimumLevel(LogLevel.Error);

            builder.AddApiDefaults();

            builder.Services
                .AddAuthentication(SessionCookieDefaults.AuthenticationScheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthentication>(
                    SessionCookieDefaults.AuthenticationScheme, _ => { })
                .AddScheme<AuthenticationSchemeOptions, TestAuthentication>(
                    BasicAuthenticationDefaults.AuthenticationScheme, _ => { });
            builder.Services
                .AddAuthorizationBuilder()
                .AddGuildAuthorizationPolicies();

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddHybridCache();
            builder.Services.AddSingleton<IAuthorizationHandler, GuildPermissionHandler>();
            builder.Services.AddTransient<IClaimsTransformation, GuildPermissionClaimTransformer>();

            // Match the production/map-request host model: the shared auth transformer uses static EF compiled queries.
            var options = new DbContextOptionsBuilder<ServerDbContext>()
                .UseNpgsql(database.Context.Database.GetConnectionString())
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            options.WithExpressionExpanding();

            builder.Services.AddScoped(_ => new ServerDbContext(options.Options));
            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddSingleton<IHeavyBackgroundTaskQueue>(queue);
            builder.Services.AddScoped<PointCurveService>();

            var app = builder.Build();

            app.UseExceptionHandler();
            app.UseStatusCodePages();
            app.UseAuthentication();
            app.UseMiddleware<SessionCookieRequestVerificationMiddleware>();
            app.UseAuthorization();

            PointCurveEndpoints.MapEndpoints(app);

            try
            {
                await app.StartAsync();

                return new Host(app, queue);
            }
            catch
            {
                await app.DisposeAsync();

                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    private static ClientRequests.UpdateSettings Settings(ClientPolicy policy)
        => new([new Sample(0, 100), new Sample(32, 100)], [new Sample(0, 0), new Sample(1, 1)], 0.25, policy);

    private static void AsPlayer(HttpClient client, string? id)
    {
        client.DefaultRequestHeaders.Remove("X-Test-Player");

        if (id is not null) client.DefaultRequestHeaders.Add("X-Test-Player", id);
    }

    [Test]
    public async Task HttpMutations_ShouldRequireGuildPermissionAndSessionVerificationHeader()
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        await database.AddPlayerAsync(2);

        await using var host = await Host.StartAsync(database);
        using var http = host.CreateClient();
        using var client = new Client::GuildSaber.CSharpClient.GuildSaberClient(http, http.BaseAddress!, null);

        var put = new ClientRequests.PutCurve([new Sample(0, 0), new Sample(0.6, 0.4), new Sample(1, 1)]);

        (await client.PointCurves.PutCurveAsync(new ContextId(2), new RankedMapId(1), 3, put))
            .Error.Should().Contain("401");

        AsPlayer(http, "2");

        (await client.PointCurves.PutCurveAsync(new ContextId(2), new RankedMapId(1), 3, put))
            .Error.Should().Contain("403");

        AsPlayer(http, "1");
        http.DefaultRequestHeaders.Remove(SessionCookieDefaults.RequestVerificationHeaderName);

        (await client.PointCurves.PutCurveAsync(new ContextId(2), new RankedMapId(1), 3, put))
            .Error.Should().Contain("403");

        http.DefaultRequestHeaders.Add(
            SessionCookieDefaults.RequestVerificationHeaderName,
            SessionCookieDefaults.RequestVerificationHeaderValue);

        (await client.PointCurves.PutCurveAsync(new ContextId(2), new RankedMapId(1), 3, put))
            .IsSuccess.Should().BeTrue();
        host.Queue.Count.Should().Be(1);

        AsPlayer(http, null);

        (await client.PointCurves.GetCurveAsync(new ContextId(2), new RankedMapId(1), 3))
            .IsSuccess.Should().BeTrue();
        (await client.PointCurves.PreviewAsync(new ContextId(2), new RankedMapId(1), 3,
                new ClientRequests.Preview(Accuracies: [0.6])))
            .Value.Values.Single().RawPoints.Should().Be(110);
    }

    [Test]
    public async Task GuildLeaderWithoutRankingTeamPermission_ShouldBeAllowedToChangeSettingsAndMapCurves()
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        await using var host = await Host.StartAsync(database, Member.EPermission.GuildLeader);
        using var http = host.CreateClient();

        AsPlayer(http, "1");
        using var client = new Client::GuildSaber.CSharpClient.GuildSaberClient(http, http.BaseAddress!, null);

        (await database.Context.Members.SingleAsync()).Permissions.Should().Be(Member.EPermission.GuildLeader);

        (await client.PointCurves.UpdateSettingsAsync(new GuildId(2), 3, Settings(new ClientPolicy.EditableMapCurve())))
            .IsSuccess.Should().BeTrue();

        (await client.PointCurves.PutCurveAsync(new ContextId(2), new RankedMapId(1), 3,
                new ClientRequests.PutCurve([new Sample(0, 0), new Sample(0.6, 0.4), new Sample(1, 1)])))
            .Value.IsMapOverride.Should().BeTrue();

        (await client.PointCurves.DeleteCurveAsync(new ContextId(2), new RankedMapId(1), 3))
            .Value.IsMapOverride.Should().BeFalse();
    }

    [Test]
    public async Task EditableHttpWorkflow_ShouldPreviewWithoutPublishingAndReturnFieldValidationErrors()
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        await using var host = await Host.StartAsync(database);
        using var http = host.CreateClient();

        AsPlayer(http, "1");
        using var client = new Client::GuildSaber.CSharpClient.GuildSaberClient(http, http.BaseAddress!, null);
        var api = client.PointCurves;

        var settings = await api.GetSettingsAsync(new GuildId(2), 3);

        settings.Value!.Policy.Should().BeOfType<ClientPolicy.EditableMapCurve>();
        settings.Value.AccuracyBonusRatio.Should().Be(0.25);

        (await api.GetCurveAsync(new ContextId(2), new RankedMapId(1), 3))
            .Value!.IsMapOverride.Should().BeFalse();

        var stored = await api.PutCurveAsync(new ContextId(2), new RankedMapId(1), 3,
            new ClientRequests.PutCurve([new Sample(0, 0), new Sample(0.6, 0.4), new Sample(1, 1)]));

        stored.Value.IsMapOverride.Should().BeTrue();
        stored.Value.Points.Should().Equal(
            new Sample(1, 1),
            new Sample(0.6, 0.4),
            new Sample(0, 0));
        stored.Value.UpdatedAt.Should().NotBeNull();

        var preview = await api.PreviewAsync(new ContextId(2), new RankedMapId(1), 3,
            new ClientRequests.Preview([new Sample(0, 0), new Sample(0.6, 0.8), new Sample(1, 1)], [0.6]));

        preview.Value.BasePoints.Should().Be(100);
        preview.Value.MaximumAdditionalPoints.Should().Be(25);
        preview.Value.Values.Single().RawPoints.Should().Be(120);
        (await api.GetCurveAsync(new ContextId(2), new RankedMapId(1), 3))
            .Value!.Points.Should().Equal(stored.Value.Points);
        host.Queue.Count.Should().Be(1, "preview must not queue a score refresh");

        var duplicateRequest = new ClientRequests.PutCurve(
            [new Sample(0, 0), new Sample(0.6, 0.2), new Sample(0.6, 0.7), new Sample(1, 1)]);
        var duplicate = await api.PutCurveAsync(new ContextId(2), new RankedMapId(1), 3, duplicateRequest);

        duplicate.Error.Should().Contain("400");

        using var duplicateResponse = await http.PutAsync("contexts/2/ranked-maps/1/points/3/curve",
            JsonContent.Create(duplicateRequest));

        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var duplicateProblem = JsonDocument.Parse(await duplicateResponse.Content.ReadAsStringAsync());

        duplicateProblem.RootElement
            .GetProperty("errors")
            .TryGetProperty("points", out _)
            .Should().BeTrue();

        var invalidAccuracyRequest = new ClientRequests.Preview(Accuracies: [60]);
        var invalidAccuracy = await api.PreviewAsync(new ContextId(2), new RankedMapId(1), 3,
            invalidAccuracyRequest);

        invalidAccuracy.Error.Should().Contain("400");

        using var invalidAccuracyResponse = await http.PostAsync("contexts/2/ranked-maps/1/points/3/curve/preview",
            JsonContent.Create(invalidAccuracyRequest));

        invalidAccuracyResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var accuracyProblem = JsonDocument.Parse(await invalidAccuracyResponse.Content.ReadAsStringAsync());

        accuracyProblem.RootElement
            .GetProperty("errors")
            .TryGetProperty("accuracies", out _)
            .Should().BeTrue();

        using var missing = await http.PutAsync("contexts/2/ranked-maps/1/points/3/curve",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        missing.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await api.DeleteCurveAsync(new ContextId(2), new RankedMapId(1), 3))
            .Value.IsMapOverride.Should().BeFalse();
        (await api.DeleteCurveAsync(new ContextId(2), new RankedMapId(1), 3))
            .Value.IsMapOverride.Should().BeFalse();
    }

    [Test]
    public async Task DifficultyOnlySettings_ShouldAllowEmptyAccuracyUntilTheBonusIsEnabled()
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        await using var host = await Host.StartAsync(database);
        using var http = host.CreateClient();

        AsPlayer(http, "1");
        using var client = new Client::GuildSaber.CSharpClient.GuildSaberClient(http, http.BaseAddress!, null);
        var api = client.PointCurves;
        var settings = Settings(new ClientPolicy.Shared()) with
        {
            AccuracyBonusRatio = 0,
            DefaultAccuracyCurve = []
        };

        (await api.UpdateSettingsAsync(new GuildId(2), 3, settings)).Value.DefaultAccuracyCurve.Should().BeEmpty();
        (await api.GetSettingsAsync(new GuildId(2), 3)).Value!.DefaultAccuracyCurve.Should().BeEmpty();
        (await api.GetCurveAsync(new ContextId(2), new RankedMapId(1), 3)).Value!.Points.Should().BeEmpty();

        var preview = await api.PreviewAsync(new ContextId(2), new RankedMapId(1), 3,
            new ClientRequests.Preview(Accuracies: [0.6, 1]));

        preview.Value.Curve.Points.Should().BeEmpty();
        preview.Value.MaximumAdditionalPoints.Should().Be(0);
        preview.Value.Values.Should().OnlyContain(x => x.RawPoints.Equals(100f) && x.BonusFraction.Equals(0d));

        (await api.UpdateSettingsAsync(new GuildId(2), 3, settings with { AccuracyBonusRatio = 0.25 }))
            .Error.Should().Contain("400");

        (await api.UpdateSettingsAsync(new GuildId(2), 3, settings with
        {
            AccuracyBonusRatio = 0.25,
            DefaultAccuracyCurve = [new Sample(0, 0), new Sample(1, 1)]
        })).IsSuccess.Should().BeTrue();

        (await api.PreviewAsync(new ContextId(2), new RankedMapId(1), 3,
                new ClientRequests.Preview(Accuracies: [0.6])))
            .Value.Values.Single().RawPoints.Should().Be(115);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task SharedAndNativePolicies_ShouldRejectManualMutationsAtTheServer(bool native)
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        await using var host = await Host.StartAsync(database);
        using var http = host.CreateClient();

        AsPlayer(http, "1");
        using var client = new Client::GuildSaber.CSharpClient.GuildSaberClient(http, http.BaseAddress!, null);

        ClientPolicy policy = native
            ? new ClientPolicy.ScoreDerivedMapCurve([0.9, 0.8, 0.7])
            : new ClientPolicy.Shared();

        (await client.PointCurves.UpdateSettingsAsync(new GuildId(2), 3, Settings(policy)))
            .IsSuccess.Should().BeTrue();
        (await client.PointCurves.GetCurveAsync(new ContextId(2), new RankedMapId(1), 3))
            .Value!.Editable.Should().BeFalse();

        var putRequest = new ClientRequests.PutCurve([new Sample(0, 0), new Sample(1, 1)]);
        var put = await client.PointCurves.PutCurveAsync(new ContextId(2), new RankedMapId(1), 3, putRequest);

        put.Error.Should().Contain("409");

        using var putResponse = await http.PutAsync("contexts/2/ranked-maps/1/points/3/curve",
            JsonContent.Create(putRequest));

        putResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var putProblem = JsonDocument.Parse(await putResponse.Content.ReadAsStringAsync());

        putProblem.RootElement
            .GetProperty("code")
            .GetString()
            .Should().Be("curve_read_only");

        var delete = await client.PointCurves.DeleteCurveAsync(new ContextId(2), new RankedMapId(1), 3);

        delete.Error.Should().Contain("409");

        using var deleteResponse = await http.DeleteAsync("contexts/2/ranked-maps/1/points/3/curve");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var deleteProblem = JsonDocument.Parse(await deleteResponse.Content.ReadAsStringAsync());

        deleteProblem.RootElement
            .GetProperty("code")
            .GetString()
            .Should().Be("curve_read_only");

        (await database.Context.RankedMapPointCurves.AnyAsync()).Should().BeFalse();
    }

    [Test]
    public async Task HttpResources_ShouldMatchRouteContextAndRequireThePointToBeAttached()
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        database.Context.AddRange(
            new Context
            {
                Id = new ContextId(3),
                GuildId = new GuildId(2),
                Info = new ContextInfo("Other", "")
            },
            new Point
            {
                Id = new Point.PointId(4),
                GuildId = new GuildId(2),
                Info = PointInfo.TryCreate("Other", "Other point").Value,
                CurveSettings = new CurveSettings
                {
                    Difficulty = new CustomCurve([(32, 100), (0, 100)]),
                    Accuracy = new CustomCurve([(1, 1), (0, 0)]),
                    AccuracyBonusRatio = 0.25,
                    MapCurvePolicy = PointCurvePolicy.EditableMapCurve
                }
            });

        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        await using var host = await Host.StartAsync(database);
        using var http = host.CreateClient();

        AsPlayer(http, "1");
        using var client = new Client::GuildSaber.CSharpClient.GuildSaberClient(http, http.BaseAddress!, null);
        var api = client.PointCurves;

        (await api.GetSettingsAsync(new GuildId(2), 4)).Value.Should().NotBeNull();
        (await api.GetSettingsAsync(new GuildId(3), 3)).Value.Should().BeNull();

        (await api.GetCurveAsync(new ContextId(3), new RankedMapId(1), 3)).Value.Should().BeNull();
        (await api.GetCurveAsync(new ContextId(2), new RankedMapId(1), 4)).Value.Should().BeNull();

        var shape = new ClientRequests.PutCurve([new Sample(0, 0), new Sample(1, 1)]);

        (await api.PutCurveAsync(new ContextId(3), new RankedMapId(1), 3, shape))
            .Error.Should().Contain("404");
        (await api.PutCurveAsync(new ContextId(2), new RankedMapId(1), 4, shape))
            .Error.Should().Contain("404");
    }
}
