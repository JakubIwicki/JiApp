using FluentAssertions;
using JiApp.Infrastructure.Services;
using Xunit;

namespace JiApp.Tests.Infrastructure;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService(
        string key = "supersecretkeythatislongenough12345678901234567890",
        string issuer = "TestIssuer",
        string audience = "TestAudience",
        int expireMinutes = 30)
    {
        return new JwtTokenService(key, issuer, audience, expireMinutes);
    }

    [Fact]
    public void GenerateToken_ProducesValidJwtString()
    {
        var service = CreateService();

        var token = service.GenerateToken(1, "testuser");

        token.Should().NotBeNullOrWhiteSpace();
        token.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public void IsTokenValid_ReturnsTrue_ForFreshToken()
    {
        var service = CreateService();

        var token = service.GenerateToken(1, "testuser");

        service.IsTokenValid(token).Should().BeTrue();
    }

    [Fact]
    public void IsTokenValid_ReturnsFalse_ForExpiredToken()
    {
        var service = CreateService(expireMinutes: -1);

        var token = service.GenerateToken(1, "testuser");

        service.IsTokenValid(token).Should().BeFalse();
    }

    [Fact]
    public void GetUsernameFromToken_ExtractsUsernameClaim()
    {
        var service = CreateService();

        var token = service.GenerateToken(1, "janek");

        var username = service.GetUsernameFromToken(token);
        username.Should().Be("janek");
    }

    [Fact]
    public void GetUserIdFromToken_ExtractsUserIdClaim()
    {
        var service = CreateService();

        var token = service.GenerateToken(99, "user99");

        var userId = service.GetUserIdFromToken(token);
        userId.Should().Be(99);
    }
}
