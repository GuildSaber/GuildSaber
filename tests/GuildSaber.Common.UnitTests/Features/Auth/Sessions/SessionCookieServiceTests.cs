using System.Net;
using AwesomeAssertions;
using GuildSaber.Api.Features.Auth.Sessions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace GuildSaber.Common.UnitTests.Features.Auth.Sessions;

public class SessionCookieServiceTests
{
    private readonly SessionCookieService _service = new(new TestHostEnvironment());

    [Test]
    public void Append_HttpResponse_CreatesLaxCookie()
    {
        var response = CreateResponse("http");

        _service.Append(response, "token", DateTimeOffset.UtcNow.AddHours(1));

        var setCookie = response.Headers.SetCookie.ToString().ToLowerInvariant();
        setCookie.Should().Contain("samesite=lax");
        setCookie.Should().NotContain("; secure");
    }

    [Test]
    public void Append_HttpsResponse_CreatesSecureCrossSiteCookie()
    {
        var response = CreateResponse("https");

        _service.Append(response, "token", DateTimeOffset.UtcNow.AddHours(1));

        var setCookie = response.Headers.SetCookie.ToString().ToLowerInvariant();
        setCookie.Should().Contain("samesite=none");
        setCookie.Should().Contain("; secure");
    }

    [Test]
    public void Append_ProductionHttpResponse_DoesNotDowngradeCookie()
    {
        var service = new SessionCookieService(new TestHostEnvironment
        {
            EnvironmentName = Environments.Production
        });
        var response = CreateResponse("http");

        service.Append(response, "token", DateTimeOffset.UtcNow.AddHours(1));

        var setCookie = response.Headers.SetCookie.ToString().ToLowerInvariant();
        setCookie.Should().Contain("samesite=none");
        setCookie.Should().Contain("; secure");
    }

    [Test]
    public void Append_HttpsResponse_CookieContainerRetainsCookie()
    {
        var uri = new Uri("https://api-dev.guildsaber.com");
        var response = CreateResponse(uri.Scheme);
        var cookies = new CookieContainer();

        _service.Append(response, "token", DateTimeOffset.UtcNow.AddHours(1));
        cookies.SetCookies(uri, response.Headers.SetCookie.ToString());

        cookies.GetCookieHeader(uri).Should().Be($"{_service.CookieName}=token");
    }

    private static HttpResponse CreateResponse(string scheme)
    {
        var context = new DefaultHttpContext { Request = { Scheme = scheme } };
        return context.Response;
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = nameof(SessionCookieServiceTests);
        public string ContentRootPath { get; set; } = "/";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}