using AwesomeAssertions;
using GuildSaber.Api.Features.Auth;

namespace GuildSaber.Common.UnitTests.Features.Auth;

public class OriginPolicyTests
{
    private static readonly string[] ConfiguredOrigins =
    [
        "https://guildsaber.com",
        "https://dev.guildsaber.com:8443/configuration-path-is-ignored"
    ];

    [Test]
    [Arguments("http://localhost")]
    [Arguments("https://LOCALHOST:5044/auth")]
    [Arguments("http://127.0.0.1:5173")]
    [Arguments("http://192.168.0.0")]
    [Arguments("https://192.168.42.123:5044/auth?from=lan")]
    [Arguments("http://192.168.255.255:65535")]
    public void IsAllowed_ShouldAllowLocalOriginsOnAnyPort(string url)
    {
        OriginPolicy.IsAllowed(url, ConfiguredOrigins).Should().BeTrue();
    }

    [Test]
    [Arguments("https://guildsaber.com/auth")]
    [Arguments("https://dev.guildsaber.com:8443/auth")]
    public void IsAllowed_ShouldAllowConfiguredOrigins(string url)
    {
        OriginPolicy.IsAllowed(url, ConfiguredOrigins).Should().BeTrue();
    }

    [Test]
    [Arguments("http://192.167.255.255")]
    [Arguments("http://192.169.0.0")]
    [Arguments("http://192.168.42.123.example.com")]
    [Arguments("ftp://192.168.42.123")]
    [Arguments("https://guildsaber.com:8443")]
    [Arguments("http://guildsaber.com")]
    [Arguments("not-a-url")]
    public void IsAllowed_ShouldRejectOriginsOutsideTheLocalAndConfiguredRanges(string url)
    {
        OriginPolicy.IsAllowed(url, ConfiguredOrigins).Should().BeFalse();
    }
}
