using JiApp.Common.Models;
using JiApp.Identity.Configuration;
using JiApp.Common.Abstractions;
using JiApp.Identity.Features.Auth.Refresh;
using JiApp.Identity.Models;
using JiApp.Identity.Services;
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

        public Mock<IRefreshTokenService> RefreshTokenServiceMock { get; } = new();
        public Mock<UserManager<User>> UserManagerMock { get; } = new(
            Mock.Of<IUserStore<User>>(),
            Mock.Of<Microsoft.Extensions.Options.IOptions<IdentityOptions>>(),
            Mock.Of<IPasswordHasher<User>>(),
            Array.Empty<IUserValidator<User>>(),
            Array.Empty<IPasswordValidator<User>>(),
            Mock.Of<ILookupNormalizer>(),
            Mock.Of<IdentityErrorDescriber>(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<User>>>());

        public Mock<IJwtTokenService> JwtTokenServiceMock { get; } = new();
        public Mock<IUserAccessService> AccessServiceMock { get; } = new();
        public Mock<IDbContextTransaction> TransactionMock { get; } = new();
        public IdentitySettings Settings { get; } = new()
        {
            Jwt = new IdentitySettings.JwtSettings { AccessTokenExpireMinutes = 15 }
        };

        public RefreshHandler Sut { get; }

        public Fixture()
        {
            RefreshTokenServiceMock
                .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(TransactionMock.Object);

            Sut = new RefreshHandler(
                RefreshTokenServiceMock.Object,
                UserManagerMock.Object,
                JwtTokenServiceMock.Object,
                AccessServiceMock.Object,
                Settings,
                Mock.Of<ILogger<RefreshHandler>>());
        }

        public Fixture WithValidRefreshToken(string rawToken = "valid-refresh-token")
        {
            var storedToken = new RefreshToken { Id = 10, Token = "raw-token", UserId = 1, IsRevoked = false };
            RefreshTokenServiceMock.Setup(x => x.ValidateAsync(rawToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(storedToken);
            UserManagerMock.Setup(x => x.FindByIdAsync("1"))
                .ReturnsAsync(_testUser);
            JwtTokenServiceMock
                .Setup(x => x.GenerateToken(_testUser.Id, _testUser.UserName!, It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>()))
                .Returns("new-access-token");
            UserManagerMock.Setup(x => x.GetRolesAsync(_testUser))
                .ReturnsAsync(["User"]);
            RefreshTokenServiceMock.Setup(x => x.RevokeAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            RefreshTokenServiceMock.Setup(x => x.CreateAsync(_testUser.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RefreshToken { Token = "new-refresh-token" });
            return this;
        }

        public Fixture WithInvalidToken(string rawToken = "invalid-token")
        {
            RefreshTokenServiceMock.Setup(x => x.ValidateAsync(rawToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync((RefreshToken?)null);
            return this;
        }

        public Fixture WithTokenForMissingUser(string rawToken = "token-for-missing-user")
        {
            var storedToken = new RefreshToken { Id = 10, Token = "raw-token", UserId = 999, IsRevoked = false };
            RefreshTokenServiceMock.Setup(x => x.ValidateAsync(rawToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(storedToken);
            UserManagerMock.Setup(x => x.FindByIdAsync("999"))
                .ReturnsAsync((User?)null);
            return this;
        }

        public Fixture WithReusedToken(string rawToken = "reused-token")
        {
            var revokedToken = new RefreshToken { Id = 10, Token = "raw-token", UserId = 1, IsRevoked = true };
            RefreshTokenServiceMock.Setup(x => x.ValidateAsync(rawToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(revokedToken);
            return this;
        }

        public Fixture WithConcurrentRevocation(string rawToken = "concurrent-token")
        {
            var storedToken = new RefreshToken { Id = 10, Token = "raw-token", UserId = 1, IsRevoked = false };
            RefreshTokenServiceMock.Setup(x => x.ValidateAsync(rawToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(storedToken);
            UserManagerMock.Setup(x => x.FindByIdAsync("1"))
                .ReturnsAsync(_testUser);
            RefreshTokenServiceMock.Setup(x => x.RevokeAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false); // already revoked by another request
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

        fixture.RefreshTokenServiceMock.Verify(x => x.RevokeAsync(10, It.IsAny<CancellationToken>()), Times.Once);
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
        fixture.RefreshTokenServiceMock.Verify(x => x.RevokeAllForUserAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_RevokesAllTokens_WhenConcurrentRevocationDetected()
    {
        var fixture = new Fixture().WithConcurrentRevocation();

        var result = await fixture.Sut.HandleAsync(new RefreshRequest("concurrent-token"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid or expired refresh token");
        fixture.RefreshTokenServiceMock.Verify(x => x.RevokeAllForUserAsync(1, It.IsAny<CancellationToken>()), Times.Once);
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
        fixture.RefreshTokenServiceMock.Setup(x => x.ValidateAsync("valid-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);
        fixture.UserManagerMock.Setup(x => x.FindByIdAsync("1"))
            .ReturnsAsync(userWithNullStamp);
        fixture.UserManagerMock
            .Setup(x => x.UpdateSecurityStampAsync(userWithNullStamp))
            .Callback<User>(u => u.SecurityStamp = "generated-stamp")
            .ReturnsAsync(IdentityResult.Success);
        fixture.JwtTokenServiceMock
            .Setup(x => x.GenerateToken(1, "testuser", It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), "generated-stamp"))
            .Returns("new-access-token");
        fixture.UserManagerMock.Setup(x => x.GetRolesAsync(userWithNullStamp))
            .ReturnsAsync(["User"]);
        fixture.RefreshTokenServiceMock.Setup(x => x.RevokeAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        fixture.RefreshTokenServiceMock.Setup(x => x.CreateAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshToken { Token = "new-refresh-token" });

        var result = await fixture.Sut.HandleAsync(new RefreshRequest("valid-token"), CancellationToken.None);

        AssertSuccess(result);
        fixture.UserManagerMock.Verify(x => x.UpdateSecurityStampAsync(userWithNullStamp), Times.Once);
        fixture.JwtTokenServiceMock.Verify(x => x.GenerateToken(1, "testuser",
            It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), "generated-stamp"), Times.Once);
    }
}
