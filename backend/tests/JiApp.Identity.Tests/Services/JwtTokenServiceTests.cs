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

        var token = sut.GenerateToken(1, "testuser", [], [], "stamp-1");

        token.Should().NotBeNullOrEmpty();
        sut.IsTokenValid(token).Should().BeTrue();
    }

    [Fact]
    public void GenerateToken_EmitsSecurityStampClaim()
    {
        var sut = new Fixture().Sut;

        var token = sut.GenerateToken(1, "testuser", [], [], "stamp-xyz");

        var allClaims = new JwtSecurityTokenHandler()
            .ReadJwtToken(token)
            .Claims;

        var stampClaim = allClaims
            .FirstOrDefault(c => c.Type == JwtTokenService.SecurityStampClaimType);

        stampClaim.Should().NotBeNull();
        stampClaim!.Value.Should().Be("stamp-xyz");
    }

    [Fact]
    public void GenerateToken_EmitsRoleAndPermissionClaims()
    {
        var sut = new Fixture().Sut;
        var roles = new[] { "User" };
        var permissions = new[] { "ytdownloader.access", "scheduler.access" };

        var token = sut.GenerateToken(1, "testuser", roles, permissions, "stamp-1");

        var allClaims = new JwtSecurityTokenHandler()
            .ReadJwtToken(token)
            .Claims;

        var roleClaims = allClaims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .ToArray();

        var permissionClaims = allClaims
            .Where(c => c.Type == "permission")
            .Select(c => c.Value)
            .ToArray();

        roleClaims.Should().BeEquivalentTo(roles);
        permissionClaims.Should().BeEquivalentTo(permissions);
    }

    [Fact]
    public void GenerateToken_EmitsNoRoleOrPermissionClaims_WhenNoneGranted()
    {
        var sut = new Fixture().Sut;

        var token = sut.GenerateToken(1, "testuser", [], [], "stamp-1");

        var roleClaims = new JwtSecurityTokenHandler()
            .ReadJwtToken(token)
            .Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role);

        var permissionClaims = new JwtSecurityTokenHandler()
            .ReadJwtToken(token)
            .Claims
            .Where(c => c.Type == "permission");

        roleClaims.Should().BeEmpty();
        permissionClaims.Should().BeEmpty();
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

        var token = sut.GenerateToken(42, "jakub", [], [], "stamp-1");
        var username = sut.GetUsernameFromToken(token);

        username.Should().Be("jakub");
    }

    [Fact]
    public void GetUserIdFromToken_ReturnsCorrectUserId()
    {
        var sut = new Fixture().Sut;

        var token = sut.GenerateToken(42, "jakub", [], [], "stamp-1");
        var userId = sut.GetUserIdFromToken(token);

        userId.Should().Be(42);
    }

    [Fact]
    public void GenerateToken_ProducesDifferentTokens_ForDifferentUsers()
    {
        var sut = new Fixture().Sut;

        var t1 = sut.GenerateToken(1, "alice", [], [], "stamp-1");
        var t2 = sut.GenerateToken(2, "bob", [], [], "stamp-2");

        t1.Should().NotBe(t2);
    }

    [Fact]
    public void IsTokenValid_ReturnsFalse_ForExpiredToken()
    {
        var sut = new Fixture(expireMinutes: -1).Sut;

        var token = sut.GenerateToken(1, "testuser", [], [], "stamp-1");
        new Fixture().Sut.IsTokenValid(token).Should().BeFalse();
    }
}
