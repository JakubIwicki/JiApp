using FluentAssertions;
using JiApp.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace JiApp.Tests.Infrastructure;

public class JwtTokenServiceTests
{
    private static IConfiguration CreateConfig(string key, string issuer, string audience, int expireMinutes)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = key,
                ["Jwt:Issuer"] = issuer,
                ["Jwt:Audience"] = audience,
                ["Jwt:ExpireMinutes"] = expireMinutes.ToString()
            })
            .Build();
    }

    [Fact]
    public void GenerateToken_ProducesValidJwtString()
    {
        var config = CreateConfig(
            "supersecretkeythatislongenough12345678901234567890",
            "TestIssuer", "TestAudience", 30);
        var service = new JwtTokenService(config);

        var token = service.GenerateToken(1, "testuser");

        token.Should().NotBeNullOrWhiteSpace();
        token.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public void IsTokenValid_ReturnsTrue_ForFreshToken()
    {
        var config = CreateConfig(
            "supersecretkeythatislongenough12345678901234567890",
            "TestIssuer", "TestAudience", 30);
        var service = new JwtTokenService(config);

        var token = service.GenerateToken(1, "testuser");

        service.IsTokenValid(token).Should().BeTrue();
    }

    [Fact]
    public void IsTokenValid_ReturnsFalse_ForExpiredToken()
    {
        var config = CreateConfig(
            "supersecretkeythatislongenough12345678901234567890",
            "TestIssuer", "TestAudience", -1);
        var service = new JwtTokenService(config);

        var token = service.GenerateToken(1, "testuser");

        service.IsTokenValid(token).Should().BeFalse();
    }

    [Fact]
    public void GetUsernameFromToken_ExtractsUsernameClaim()
    {
        var config = CreateConfig(
            "supersecretkeythatislongenough12345678901234567890",
            "TestIssuer", "TestAudience", 30);
        var service = new JwtTokenService(config);

        var token = service.GenerateToken(1, "janek");

        var username = service.GetUsernameFromToken(token);
        username.Should().Be("janek");
    }

    [Fact]
    public void GetUserIdFromToken_ExtractsUserIdClaim()
    {
        var config = CreateConfig(
            "supersecretkeythatislongenough12345678901234567890",
            "TestIssuer", "TestAudience", 30);
        var service = new JwtTokenService(config);

        var token = service.GenerateToken(99, "user99");

        var userId = service.GetUserIdFromToken(token);
        userId.Should().Be(99);
    }
}
