using System.Security.Claims;
using JiApp.Common.Services;
using Microsoft.AspNetCore.Http;

namespace JiApp.Common.Tests.Services;

public sealed class CurrentUserServiceTests
{
    [Fact]
    public void ReturnsUserId_FromNameIdentifierClaim()
    {
        var service = CreateService(UserWithClaims(new Claim(ClaimTypes.NameIdentifier, "42")));

        service.UserId.Should().Be(42);
    }

    [Fact]
    public void ReturnsUsername_FromNameClaim()
    {
        var service = CreateService(UserWithClaims(new Claim(ClaimTypes.Name, "alice")));

        service.Username.Should().Be("alice");
    }

    [Fact]
    public void ThrowsUnauthorized_WhenNameIdentifierClaimMissing()
    {
        var service = CreateService(UserWithClaims(new Claim(ClaimTypes.Name, "alice")));

        var act = () => service.UserId;

        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void ThrowsUnauthorized_WhenNameIdentifierClaimNotNumeric()
    {
        var service = CreateService(UserWithClaims(new Claim(ClaimTypes.NameIdentifier, "not-a-number")));

        var act = () => service.UserId;

        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void ThrowsUnauthorized_WhenHttpContextMissing()
    {
        var service = new CurrentUserService(new HttpContextAccessor());

        var act = () => service.UserId;

        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void ReturnsEmptyUsername_WhenNameClaimMissing()
    {
        var service = CreateService(UserWithClaims(new Claim(ClaimTypes.NameIdentifier, "42")));

        service.Username.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsEmptyUsername_WhenHttpContextMissing()
    {
        var service = new CurrentUserService(new HttpContextAccessor());

        service.Username.Should().BeEmpty();
    }

    [Fact]
    public void EvaluatesEachClaimOnlyOnce()
    {
        var user = new CountingClaimsPrincipal(
            new Claim(ClaimTypes.NameIdentifier, "42"),
            new Claim(ClaimTypes.Name, "alice"));
        var service = new CurrentUserService(CreateAccessor(user));

        _ = service.UserId;
        _ = service.Username;
        _ = service.UserId;
        _ = service.Username;

        user.FindFirstCountFor(ClaimTypes.NameIdentifier).Should().Be(1);
        user.FindFirstCountFor(ClaimTypes.Name).Should().Be(1);
    }

    private static CurrentUserService CreateService(ClaimsPrincipal user) =>
        new(CreateAccessor(user));

    private static HttpContextAccessor CreateAccessor(ClaimsPrincipal user) =>
        new() { HttpContext = new DefaultHttpContext { User = user } };

    private static ClaimsPrincipal UserWithClaims(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    private sealed class CountingClaimsPrincipal : ClaimsPrincipal
    {
        private readonly Dictionary<string, int> _lookups = [];

        public CountingClaimsPrincipal(params Claim[] claims)
        {
            AddIdentity(new ClaimsIdentity(claims, authenticationType: "TestAuth"));
        }

        public int FindFirstCountFor(string claimType) => _lookups.GetValueOrDefault(claimType);

        public override Claim? FindFirst(string claimType)
        {
            _lookups[claimType] = _lookups.GetValueOrDefault(claimType) + 1;
            return base.FindFirst(claimType);
        }
    }
}
