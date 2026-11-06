extern alias Client;
using System.Net;
using System.Text;
using AwesomeAssertions;
using GuildSaber.Common.StrongTypes;
using ClientRequests = Client::GuildSaber.Api.Features.PointCurves.Http.PointCurveRequests;
using Sample = Client::GuildSaber.Api.Features.PointCurves.Http.PointCurveRequests.Sample;

namespace GuildSaber.Common.UnitTests.Features.PointCurves;

public class PointCurveClientTests
{
    private sealed class Handler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => response(request, cancellationToken);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body)
        => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    [Test]
    public async Task Client_ShouldReturnContextualFailuresForServerErrors()
    {
        using var handler = new Handler((_, _) => Task.FromResult(Json(HttpStatusCode.InternalServerError, "{}")));
        using var http = new HttpClient(handler);
        http.BaseAddress = new Uri("https://guildsaber.test/");
        using var client = new Client::GuildSaber.CSharpClient.GuildSaberClient(http, http.BaseAddress, null);

        var read = await client.PointCurves.GetCurveAsync(new ContextId(2), new RankedMapId(1), 3);
        var write = await client.PointCurves.PutCurveAsync(new ContextId(2), new RankedMapId(1), 3,
            new ClientRequests.PutCurve([new Sample(0, 0), new Sample(1, 1)]));

        read.Error.Should().Contain("500").And.Contain("map 1, point 3, context 2");
        write.Error.Should().Contain("500").And.Contain("map 1, point 3, context 2");
    }

    [Test]
    [Arguments("not-json")]
    [Arguments("")]
    public async Task Client_ShouldReturnJsonFailuresForMalformedOrEmptySuccessBodies(string body)
    {
        using var handler = new Handler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, body)));
        using var http = new HttpClient(handler);
        http.BaseAddress = new Uri("https://guildsaber.test/");
        using var client = new Client::GuildSaber.CSharpClient.GuildSaberClient(http, http.BaseAddress, null);

        (await client.PointCurves.GetSettingsAsync(new GuildId(2), 3)).Error.Should().NotBeNullOrWhiteSpace();
        (await client.PointCurves.PutCurveAsync(new ContextId(2), new RankedMapId(1), 3,
                new ClientRequests.PutCurve([new Sample(0, 0), new Sample(1, 1)])))
            .Error.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Client_ShouldPropagateTransportFailuresAndCancellation()
    {
        using var handler = new Handler((_, token) =>
        {
            token.ThrowIfCancellationRequested();

            throw new HttpRequestException("Connection unavailable.");
        });

        using var http = new HttpClient(handler);
        http.BaseAddress = new Uri("https://guildsaber.test/");
        using var client = new Client::GuildSaber.CSharpClient.GuildSaberClient(http, http.BaseAddress, null);

        var transportRequest = client.PointCurves.GetSettingsAsync(new GuildId(2), 3);
        Func<Task> transport = () => transportRequest;

        await transport.Should().ThrowAsync<HttpRequestException>().WithMessage("Connection unavailable.");

        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var request = client.PointCurves.GetSettingsAsync(new GuildId(2), 3, cancellation.Token);
        Func<Task> cancelled = () => request;

        await cancelled.Should().ThrowAsync<OperationCanceledException>();
    }
}
