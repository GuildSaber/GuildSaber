extern alias Client;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using AwesomeAssertions;
using Client::GuildSaber.Api.Shared;
using GuildSaber.Api.Features.Auth;
using GuildSaber.Api.Features.Auth.Authorization;
using GuildSaber.Api.Features.Auth.CustomApiKey;
using GuildSaber.Api.Features.Auth.CustomApiKey.Interfaces;
using GuildSaber.Api.Features.Auth.CustomApiKey.ValidationTypes;
using GuildSaber.Api.Features.Auth.Sessions;
using GuildSaber.Api.Features.MapRequests;
using GuildSaber.Api.Features.MapRequests.Http;
using GuildSaber.Api.Features.RankedMaps.Http;
using GuildSaber.Api.Features.Songs;
using GuildSaber.Api.Setup;
using GuildSaber.Common.Services.BeatLeader;
using GuildSaber.Common.Services.BeatSaver;
using GuildSaber.Common.Services.ScoreSaber;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds.Members;
using GuildSaber.Database.Models.Server.MapRequests;
using GuildSaber.Database.Models.Server.Players;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ClientRequests = Client::GuildSaber.Api.Features.MapRequests.Http.MapProposalRequests;
using ClientResponses = Client::GuildSaber.Api.Features.MapRequests.Http.MapRequestResponses;

namespace GuildSaber.Common.UnitTests.Features.MapRequests;

public partial class MapProposalServiceTests
{
    private sealed class RequestTestAuthentication(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            => Task.FromResult(Request.Headers["X-Test-Player"].FirstOrDefault() is { } id
                ? AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(AuthConstants.PlayerIdClaimType, id)], Scheme.Name)), Scheme.Name))
                : AuthenticateResult.NoResult());
    }

    private sealed class RequestTestApiKeys : ICustomApiKeyAuthenticationService
    {
        public Task<AuthenticateResult> AuthenticateAsync(BasicCredential credential, IPAddress? clientIp)
            => Task.FromResult(credential is { User: "1" or "2", Password: "test-key" }
                ? AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(AuthConstants.PlayerIdClaimType, credential.User)],
                        BasicAuthenticationDefaults.AuthenticationScheme)),
                    BasicAuthenticationDefaults.AuthenticationScheme))
                : AuthenticateResult.Fail("Invalid API key."));
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task HttpClientWorkflow_ShouldPublishVoteBoostAcceptAndExposeHistory(bool manager)
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.Context.Players.ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsManager, manager));
        if (!manager)
            database.Context.Members.Add(new Member
            {
                GuildId = new GuildId(1), PlayerId = new PlayerId(1), CreatedAt = _createdAt, EditedAt = _createdAt,
                Permissions = Member.EPermission.GuildLeader, JoinState = Member.EJoinState.Joined, Priority = 0
            });
        database.Context.Players.Add(new Player
        {
            Id = new PlayerId(2), Info = new PlayerInfo("Community member", "", "", _createdAt),
            Platform = default, LinkedAccounts = default, SubscriptionInfo = default
        });
        await database.Context.SaveChangesAsync();
        var queue = new RecordingQueue();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.AddApiDefaults();
        builder.Services.AddAuthentication(SessionCookieDefaults.AuthenticationScheme)
            .AddScheme<AuthenticationSchemeOptions, RequestTestAuthentication>(
                SessionCookieDefaults.AuthenticationScheme, _ => { })
            .AddScheme<AuthenticationSchemeOptions, CustomApiKeyAuthenticationHandler>(
                BasicAuthenticationDefaults.AuthenticationScheme, _ => { });
        builder.Services.AddSingleton<ICustomApiKeyAuthenticationService, RequestTestApiKeys>();
        builder.Services.AddAuthorizationBuilder()
            .SetDefaultPolicy(new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(SessionCookieDefaults.AuthenticationScheme,
                    BasicAuthenticationDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser().Build())
            .AddGuildAuthorizationPolicies().AddManagerAuthorizationPolicy().AddOptionalAuthenticationPolicy();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHybridCache();
        builder.Services.AddSingleton<IAuthorizationHandler, GuildPermissionHandler>();
        builder.Services.AddTransient<IClaimsTransformation, GuildPermissionClaimTransformer>();
        builder.Services.AddScoped<ServerDbContext>(_ => database.CreateContext());
        builder.Services.AddSingleton<TimeProvider>(new FixedTimeProvider());
        builder.Services.AddSingleton(new BeatLeaderApi(_unusedHttpClient));
        builder.Services.AddSingleton(new BeatSaverApi(_unusedHttpClient));
        builder.Services.AddSingleton(new ScoreSaberApi(_unusedHttpClient));
        builder.Services.AddScoped<SongService>();
        builder.Services.AddMapRequestsFeature();
        builder.Services.AddScoped(provider
            => CreateReviewService(provider.GetRequiredService<ServerDbContext>(), queue));
        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        MapRequestEndpoints.MapEndpoints(app);
        RankedMapEndpoints.MapEndpoints(app);
        await app.StartAsync();
        try
        {
            var uri = new Uri(app.Urls.Single());
            using var client = new Client::GuildSaber.CSharpClient.GuildSaberClient(uri, uri, null);
            var api = client.MapRequests;
            var hidden = await api.GetThreadAsync(new ContextId(1), new MapRequestThreadId(1));
            hidden.IsSuccess.Should().BeTrue();
            hidden.Value.Should().BeNull();
            (await api.SubmitAsync(new ContextId(1), new MapRequestThreadId(1), new MapProposalId(1))).Error.Status
                .Should().Be(401);
            client.HttpClient.DefaultRequestHeaders.Add("X-Test-Player", "1");
            (await api.GetQueueAsync(new ContextId(1), new ClientRequests.Filters(),
                new PaginatedRequestOptions<ClientRequests.EMapRequestSorter>(Page: 0))).Error.Status.Should().Be(400);
            var mine = await api.GetMineAsync(new ContextId(1), new ClientRequests.Filters(),
                new PaginatedRequestOptions<ClientRequests.EMapProposalSorter>());
            mine.IsSuccess.Should().BeTrue();
            mine.Value.Data.Should().ContainSingle().Which.Should().BeOfType<ClientResponses.Proposal.Draft>();
            client.HttpClient.DefaultRequestHeaders.Remove("X-Test-Player");
            client.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes("1:test-key")));
            (await api.GetThreadAsync(new ContextId(1), new MapRequestThreadId(1))).Value!.Proposals.Should()
                .ContainSingle();
            (await api.GetProposalAsync(new ContextId(1), new MapRequestThreadId(1), new MapProposalId(1))).Value
                .Should()
                .BeOfType<ClientResponses.Proposal.Draft>();
            (await api.GetMineAsync(new ContextId(1), new ClientRequests.Filters(),
                new PaginatedRequestOptions<ClientRequests.EMapProposalSorter>())).Value.Data.Should().ContainSingle();
            client.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes("2:test-key")));
            (await api.GetThreadAsync(new ContextId(1), new MapRequestThreadId(1))).Value.Should().BeNull();
            (await api.GetProposalAsync(new ContextId(1), new MapRequestThreadId(1), new MapProposalId(1))).Value
                .Should().BeNull();
            client.HttpClient.DefaultRequestHeaders.Authorization = null;
            client.HttpClient.DefaultRequestHeaders.Add("X-Test-Player", "1");
            var submitted = await api.SubmitAsync(new ContextId(1), new MapRequestThreadId(1), new MapProposalId(1));
            submitted.IsSuccess.Should().BeTrue();
            submitted.Value.Should().BeOfType<ClientResponses.Proposal.Submitted>();
            client.HttpClient.DefaultRequestHeaders.Remove("X-Test-Player");
            client.HttpClient.DefaultRequestHeaders.Add("X-Test-Player", "2");
            (await api.VoteCommunityAsync(new ContextId(1), new MapRequestThreadId(1), new MapProposalId(1),
                    new ClientRequests.CommunityVote(true)))
                .IsSuccess.Should().BeTrue();
            (await api.VoteRankingTeamAsync(new ContextId(1), new MapRequestThreadId(1), new MapProposalId(1),
                    new ClientRequests.RankingTeamVote(true, null)))
                .Error.Status.Should().Be(403);
            (await api.RemoveRankingTeamVoteAsync(new ContextId(1), new MapRequestThreadId(1), new MapProposalId(1)))
                .Error.Status.Should().Be(403);
            (await api.AcceptAsync(new ContextId(1), new MapRequestThreadId(1), new MapProposalId(1))).Error.Status
                .Should().Be(403);
            (await api.DenyAsync(new ContextId(1), new MapRequestThreadId(1), new MapProposalId(1))).Error.Status
                .Should().Be(403);
            (await api.RemoveCommunityVoteAsync(new ContextId(1), new MapRequestThreadId(1), new MapProposalId(1)))
                .IsSuccess.Should().BeTrue();
            client.HttpClient.DefaultRequestHeaders.Remove("X-Test-Player");
            client.HttpClient.DefaultRequestHeaders.Add("X-Test-Player", "1");
            using var missingVoteValue = await client.HttpClient.PutAsync(
                "contexts/1/map-requests/1/proposals/1/votes/community/@me",
                new StringContent("{}", Encoding.UTF8, "application/json"));
            missingVoteValue.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await api.VoteRankingTeamAsync(new ContextId(1), new MapRequestThreadId(1), new MapProposalId(1),
                    new ClientRequests.RankingTeamVote(true, "Reviewed")))
                .IsSuccess.Should().BeTrue();
            (await api.VoteCommunityAsync(new ContextId(1), new MapRequestThreadId(1), new MapProposalId(1),
                    new ClientRequests.CommunityVote(true)))
                .IsSuccess.Should().BeTrue();
            (await api.RemoveCommunityVoteAsync(new ContextId(1), new MapRequestThreadId(1), new MapProposalId(1)))
                .IsSuccess.Should().BeTrue();
            (await api.BoostAsync(new ContextId(1), new MapRequestThreadId(1))).IsSuccess.Should().BeTrue();
            (await api.GetBoostsAsync()).Value.Allocations.Should().ContainSingle();
            client.HttpClient.DefaultRequestHeaders.Remove("X-Test-Player");
            client.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes("1:test-key")));
            var boosted =
                (await api.GetQueueAsync(new ContextId(1), new ClientRequests.Filters(),
                    new PaginatedRequestOptions<ClientRequests.EMapRequestSorter>())).Value.Data.Should()
                .ContainSingle().Which;
            boosted.BoostCount.Should().Be(1);
            boosted.IsBoostedByMe.Should().BeTrue();
            (await api.GetQueueAsync(new ContextId(1), new ClientRequests.Filters(IsRevision: true),
                new PaginatedRequestOptions<ClientRequests.EMapRequestSorter>())).Value.Data.Should().BeEmpty();
            (await api.GetQueueAsync(new ContextId(1), new ClientRequests.Filters(IsRevision: false),
                new PaginatedRequestOptions<ClientRequests.EMapRequestSorter>())).Value.Data.Should().ContainSingle();
            (await api.GetQueueAsync(new ContextId(1), new ClientRequests.Filters(Search: "nonexistent & song"),
                new PaginatedRequestOptions<ClientRequests.EMapRequestSorter>())).Value.Data.Should().BeEmpty();
            await foreach (var page in api.GetQueueAsyncEnumerable(new ContextId(1), new ClientRequests.Filters(),
                               new PaginatedRequestOptions<ClientRequests.EMapRequestSorter>(MaxPage: 1)))
                page.Value.Should().ContainSingle();
            client.HttpClient.DefaultRequestHeaders.Authorization = null;
            (await api.GetQueueAsync(new ContextId(1), new ClientRequests.Filters(),
                    new PaginatedRequestOptions<ClientRequests.EMapRequestSorter>())).Value.Data.Single().IsBoostedByMe
                .Should().BeFalse();
            client.HttpClient.DefaultRequestHeaders.Add("X-Test-Player", "1");

            var accepted = await api.AcceptAsync(new ContextId(1), new MapRequestThreadId(1), new MapProposalId(1));
            accepted.IsSuccess.Should().BeTrue();
            accepted.Value.Proposal.Votes.Should().ContainSingle().Which.Should()
                .BeOfType<ClientResponses.Vote.RankingTeam>();
            (await api.GetBoostsAsync()).Value.Allocations.Should().BeEmpty();
            (await api.GetQueueAsync(new ContextId(1), new ClientRequests.Filters(),
                new PaginatedRequestOptions<ClientRequests.EMapRequestSorter>())).Value.Data.Should().BeEmpty();
            (await api.GetThreadAsync(new ContextId(1), new MapRequestThreadId(1))).Value!.RankedMapId.Should()
                .Be(accepted.Value.RankedMapId);
            (await client.RankedMaps.GetByIdAsync(accepted.Value.RankedMapId)).Value!.ThreadId.Should()
                .Be(new MapRequestThreadId(1));
            (await api.AcceptAsync(new ContextId(1), new MapRequestThreadId(1), new MapProposalId(1))).Error.Code
                .Should().Be("invalid_state");
            (await api.RemoveRankingTeamVoteAsync(new ContextId(1), new MapRequestThreadId(1), new MapProposalId(1)))
                .Error.Code.Should().Be("invalid_state");

            var db = database.Context;
            db.MapRequestThreads.Add(new MapRequestThread
            {
                Id = new MapRequestThreadId(2), ContextId = new ContextId(1), AuthorId = new PlayerId(1),
                CreatedAt = _createdAt
            });
            var alternative = CreateDraft(2);
            db.MapProposals.Add(alternative);
            db.Entry(alternative).Property(x => x.ThreadId).CurrentValue = new MapRequestThreadId(2);
            await db.SaveChangesAsync();
            (await api.SubmitAsync(new ContextId(1), new MapRequestThreadId(1), new MapProposalId(2))).Error.Status
                .Should().Be(404);
            var duplicate = await api.SubmitAsync(new ContextId(1), new MapRequestThreadId(2), new MapProposalId(2));
            duplicate.Error.Code.Should().Be("duplicate_maps");
            duplicate.Error.Duplicates.Should().ContainSingle().Which.RankedMapId.Should()
                .Be(accepted.Value.RankedMapId);
            (await api.SubmitAsync(new ContextId(1), new MapRequestThreadId(2), new MapProposalId(2), force: true))
                .IsSuccess.Should().BeTrue();
            (await api.CloseAsync(new ContextId(1), new MapRequestThreadId(2), new MapProposalId(2), "Changed my mind"))
                .Value.Should()
                .BeOfType<ClientResponses.Proposal.Closed>();
            using var removedEndpoint = await client.HttpClient.PostAsync("contexts/1/ranked-maps", null);
            removedEndpoint.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
            queue.Count.Should().Be(1);
        }
        finally
        {
            await app.StopAsync();
        }
    }
}