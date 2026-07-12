using JiApp.Common.Models;
using JiApp.Identity.Configuration;
using JiApp.Common.Abstractions;
using JiApp.Identity.Features.Auth.Refresh;
using JiApp.Identity.Models;
using JiApp.Identity.Services;
using JiApp.Identity.Tests.Mocks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Identity.Tests.Features.Auth.Refresh;

public sealed class RefreshHandlerTests
{
    private sealed class Fixture
    {
        private readonly User _testUser = new()
        {
            Id = 1,
            UserName = "testuser",
            DisplayName = "Test User",
            SecurityStamp = "stamp",
            ConcurrencyStamp = "concurrency"
        };

        public MockRefreshTokenService RefreshTokenDouble { get; } = MockRefreshTokenService.GetSuccessful();
        public MockUserManager UserManagerDouble { get; } = MockUserManager.GetSuccessful();
        public MockJwtTokenService JwtTokenDouble { get; } = MockJwtTokenService.GetSuccessful();
        public MockUserAccessService AccessServiceDouble { get; } = MockUserAccessService.GetSuccessful();
        public Mock<IDbContextTransaction> TransactionMock { get; } = new();
        public IdentitySettings Settings { get; } = new()
        {
            Jwt = new IdentitySettings.JwtSettings { AccessTokenExpireMinutes = 15 }
        };

        public RefreshHandler Sut { get; }

        public Fixture()
        {
            RefreshTokenDouble.WithBeginTransactionAsync(TransactionMock.Object);

            Sut = new RefreshHandler(
                RefreshTokenDouble.Object,
                UserManagerDouble,
                JwtTokenDouble.Object,
                AccessServiceDouble.Object,
                Settings,
                Mock.Of<ILogger<RefreshHandler>>());
        }

        public Fixture WithValidRefreshToken(string rawToken = "valid-refresh-token")
        {
            var storedToken = new RefreshToken { Id = 10, Token = "raw-token", UserId = 1, IsRevoked = false };
            RefreshTokenDouble.WithValidateAsync(rawToken, storedToken);
            UserManagerDouble.WithFindByIdAsync("1", _testUser);
            JwtTokenDouble.WithGenerateTokenAny("new-access-token");
            UserManagerDouble.WithGetRolesAsync(_testUser, ["User"]);
            RefreshTokenDouble.WithRevokeAsync(10, true);
            RefreshTokenDouble.WithCreateAsync(_testUser.Id, new RefreshToken { Token = "new-refresh-token" });
            return this;
        }

        public Fixture WithInvalidToken(string rawToken = "invalid-token")
        {
            RefreshTokenDouble.WithValidateAsync(rawToken, null);
            return this;
        }

        public Fixture WithTokenForMissingUser(string rawToken = "token-for-missing-user")
        {
            var storedToken = new RefreshToken { Id = 10, Token = "raw-token", UserId = 999, IsRevoked = false };
            RefreshTokenDouble.WithValidateAsync(rawToken, storedToken);
            UserManagerDouble.WithFindByIdAsync("999", null);
            return this;
        }

        public Fixture WithReusedToken(string rawToken = "reused-token")
        {
            var revokedToken = new RefreshToken { Id = 10, Token = "raw-token", UserId = 1, IsRevoked = true };
            RefreshTokenDouble.WithValidateAsync(rawToken, revokedToken);
            return this;
        }

        public Fixture WithConcurrentRevocation(string rawToken = "concurrent-token")
        {
            var storedToken = new RefreshToken { Id = 10, Token = "raw-token", UserId = 1, IsRevoked = false };
            RefreshTokenDouble.WithValidateAsync(rawToken, storedToken);
            UserManagerDouble.WithFindByIdAsync("1", _testUser);
            RefreshTokenDouble.WithRevokeAsync(10, false);
            return this;
        }
    }

    [Fact]
    public async Task HandleAsync_ReturnsNewTokens_ForValidRefreshToken()
    {
        var fixture = new Fixture().WithValidRefreshToken();

        var result = await fixture.Sut.HandleAsync(new RefreshRequest("valid-refresh-token"), CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().Be("new-access-token");
        result.Value.RefreshToken.Should().Be("new-refresh-token");
        result.Value.ExpiresIn.Should().Be(900);
    }

    [Fact]
    public async Task HandleAsync_RevokesOldToken_WhenIssuingNewTokens()
    {
        var fixture = new Fixture().WithValidRefreshToken();

        await fixture.Sut.HandleAsync(new RefreshRequest("valid-refresh-token"), CancellationToken.None);

        fixture.RefreshTokenDouble.VerifyRevokedToken(10);
    }

    [Fact]
    public async Task HandleAsync_ReturnsFailure_ForInvalidToken()
    {
        var fixture = new Fixture().WithInvalidToken();

        var result = await fixture.Sut.HandleAsync(new RefreshRequest("invalid-token"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid or expired refresh token");
    }

    [Fact]
    public async Task HandleAsync_ReturnsFailure_WhenUserNotFound()
    {
        var fixture = new Fixture().WithTokenForMissingUser();

        var result = await fixture.Sut.HandleAsync(new RefreshRequest("token-for-missing-user"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User not found");
    }

    [Fact]
    public async Task HandleAsync_RevokesAllTokens_OnReuseDetection()
    {
        var fixture = new Fixture().WithReusedToken();

        var result = await fixture.Sut.HandleAsync(new RefreshRequest("reused-token"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid or expired refresh token");
        fixture.RefreshTokenDouble.VerifyRevokedAllForUser(1);
    }

    [Fact]
    public async Task HandleAsync_RevokesAllTokens_WhenConcurrentRevocationDetected()
    {
        var fixture = new Fixture().WithConcurrentRevocation();

        var result = await fixture.Sut.HandleAsync(new RefreshRequest("concurrent-token"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid or expired refresh token");
        fixture.RefreshTokenDouble.VerifyRevokedAllForUser(1);
    }

    [Fact]
    public async Task HandleAsync_PopulatesSecurityStamp_WhenNull_AndGeneratesToken()
    {
        var fixture = new Fixture();
        var storedToken = new RefreshToken { Id = 10, Token = "raw-token", UserId = 1, IsRevoked = false };
        var userWithNullStamp = new User
        {
            Id = 1,
            UserName = "testuser",
            SecurityStamp = null
        };
        fixture.RefreshTokenDouble.WithValidateAsync("valid-token", storedToken);
        fixture.UserManagerDouble.WithFindByIdAsync("1", userWithNullStamp);
        fixture.UserManagerDouble.WithUpdateSecurityStampAsync(userWithNullStamp, IdentityResult.Success,
            callback: u => u.SecurityStamp = "generated-stamp");
        fixture.JwtTokenDouble.WithGenerateToken(1, "testuser",
            It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), "generated-stamp",
            "new-access-token");
        fixture.UserManagerDouble.WithGetRolesAsync(userWithNullStamp, ["User"]);
        fixture.RefreshTokenDouble.WithRevokeAsync(10, true);
        fixture.RefreshTokenDouble.WithCreateAsync(1, new RefreshToken { Token = "new-refresh-token" });

        var result = await fixture.Sut.HandleAsync(new RefreshRequest("valid-token"), CancellationToken.None);

        AssertSuccess(result);
        fixture.UserManagerDouble.VerifyUpdatedSecurityStamp(userWithNullStamp);
        fixture.JwtTokenDouble.VerifyGeneratedToken(1, "testuser", "generated-stamp");
    }
}
