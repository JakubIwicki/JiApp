using System.IdentityModel.Tokens.Jwt;
using JiApp.Identity.Services;

namespace JiApp.Identity.Tests.Services;

public sealed class JwtTokenServiceTests
{
    private const string Key = "this-is-a-test-key-that-is-at-least-32-bytes-long!";
    private const string Issuer = "JiApp-Identity";
    private const string Audience = "jiapp-gateway";
    private const int ExpireMinutes = 15;

    private sealed class Fixture
    {
        public JwtTokenService Sut { get; }

        public Fixture(string key = Key, string issuer = Issuer, string audience = Audience,
            int expireMinutes = ExpireMinutes)
        {
            Sut = new JwtTokenService(key, issuer, audience, expireMinutes);
        }
    }

    [Fact]
    public void GenerateToken_ReturnsValidJwt()
    {
        var sut = new Fixture().Sut;

        var token = sut.GenerateToken(1, "testuser", []);

        token.Should().NotBeNullOrEmpty();
        sut.IsTokenValid(token).Should().BeTrue();
    }

    [Fact]
    public void GenerateToken_EmitsModuleClaim_PerGrantedModule()
    {
        var sut = new Fixture().Sut;
        var modules = new[] { "YtDownloader", "Scheduler" };

        var token = sut.GenerateToken(1, "testuser", modules);

        var moduleClaims = new JwtSecurityTokenHandler()
            .ReadJwtToken(token)
            .Claims
            .Where(c => c.Type == "module")
            .Select(c => c.Value)
            .ToArray();

        moduleClaims.Should().BeEquivalentTo(modules);
    }

    [Fact]
    public void GenerateToken_EmitsNoModuleClaims_WhenNoneGranted()
    {
        var sut = new Fixture().Sut;

        var token = sut.GenerateToken(1, "testuser", []);

        var moduleClaims = new JwtSecurityTokenHandler()
            .ReadJwtToken(token)
            .Claims
            .Where(c => c.Type == "module");

        moduleClaims.Should().BeEmpty();
    }

    [Fact]
    public void IsTokenValid_ReturnsFalse_ForGarbageToken()
    {
        var sut = new Fixture().Sut;

        sut.IsTokenValid("not-a-jwt").Should().BeFalse();
    }

    [Fact]
    public void GetUsernameFromToken_ReturnsCorrectUsername()
    {
        var sut = new Fixture().Sut;

        var token = sut.GenerateToken(42, "jakub", []);
        var username = sut.GetUsernameFromToken(token);

        username.Should().Be("jakub");
    }

    [Fact]
    public void GetUserIdFromToken_ReturnsCorrectUserId()
    {
        var sut = new Fixture().Sut;

        var token = sut.GenerateToken(42, "jakub", []);
        var userId = sut.GetUserIdFromToken(token);

        userId.Should().Be(42);
    }

    [Fact]
    public void GenerateToken_ProducesDifferentTokens_ForDifferentUsers()
    {
        var sut = new Fixture().Sut;

        var t1 = sut.GenerateToken(1, "alice", []);
        var t2 = sut.GenerateToken(2, "bob", []);

        t1.Should().NotBe(t2);
    }

    [Fact]
    public void IsTokenValid_ReturnsFalse_ForExpiredToken()
    {
        var sut = new Fixture(expireMinutes: -1).Sut;

        var token = sut.GenerateToken(1, "testuser", []);
        new Fixture().Sut.IsTokenValid(token).Should().BeFalse();
    }
}
