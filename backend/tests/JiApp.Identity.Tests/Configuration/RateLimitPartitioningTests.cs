using System.Net;
using System.Security.Claims;
using JiApp.Identity.Configuration;
using Microsoft.AspNetCore.Http;

namespace JiApp.Identity.Tests.Configuration;

public sealed class RateLimitPartitioningTests
{
    private sealed class Fixture
    {
        public HttpContext Sut { get; }

        private Fixture(HttpContext httpContext) => Sut = httpContext;

        public static Fixture Init(Action<HttpContext> configure)
        {
            var context = new DefaultHttpContext();
            configure(context);
            return new Fixture(context);
        }
    }

    private static void Authenticate(HttpContext context, string subject)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, subject)],
            authenticationType: "Test");
        context.User = new ClaimsPrincipal(identity);
    }

    [Fact]
    public void GetPartitionKey_WithAuthenticatedUser_ReturnsUserPrefixedSubject()
    {
        var fixture = Fixture.Init(context => Authenticate(context, "42"));

        var key = RateLimitPartitioning.GetPartitionKey(fixture.Sut);

        key.Should().Be("user:42");
    }

    [Fact]
    public void GetPartitionKey_WithAnonymousRequestAndRemoteIp_ReturnsIpPrefixedAddress()
    {
        var fixture = Fixture.Init(context =>
            context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.5"));

        var key = RateLimitPartitioning.GetPartitionKey(fixture.Sut);

        key.Should().Be("ip:192.168.1.5");
    }

    [Fact]
    public void GetPartitionKey_WithAnonymousRequestAndNoRemoteIp_ReturnsIpUnknown()
    {
        var fixture = Fixture.Init(_ => { });

        var key = RateLimitPartitioning.GetPartitionKey(fixture.Sut);

        key.Should().Be("ip:unknown");
    }
}
