using System.Globalization;
using JiApp.Common.Models;
using JiApp.Identity.Configuration;
using JiApp.Identity.Features.Auth.Refresh;
using JiApp.Identity.Models;
using JiApp.Identity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Identity.Tests.Features.Auth.Refresh;

public class RefreshHandlerTests
{
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock;
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;
    private readonly Mock<IUserModuleGrantService> _grantServiceMock;
    private readonly Mock<IDbContextTransaction> _transactionMock;
    private readonly IdentitySettings _settings;
    private readonly User _testUser;
    private readonly RefreshHandler _sut;

    public RefreshHandlerTests()
    {
        _testUser = new User
        {
            Id = 1,
            UserName = "testuser",
            DisplayName = "Test User",
            SecurityStamp = "stamp",
            ConcurrencyStamp = "concurrency"
        };

        _refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        _userManagerMock = CreateUserManagerMock();
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
        _grantServiceMock = new Mock<IUserModuleGrantService>();
        _transactionMock = new Mock<IDbContextTransaction>();

        _refreshTokenServiceMock
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        _settings = new IdentitySettings
        {
            Jwt = new IdentitySettings.JwtSettings { AccessTokenExpireMinutes = 15 }
        };

        var logger = Mock.Of<ILogger<RefreshHandler>>();

        _sut = new RefreshHandler(
            _refreshTokenServiceMock.Object,
            _userManagerMock.Object,
            _jwtTokenServiceMock.Object,
            _grantServiceMock.Object,
            _settings,
            logger);
    }

    [Fact]
    public async Task HandleAsync_returns_new_tokens_for_valid_refresh_token()
    {
        var storedToken = new RefreshToken { Id = 10, Token = "raw-token", UserId = 1 };
        _refreshTokenServiceMock.Setup(x => x.ValidateAsync("valid-refresh-token"))
            .ReturnsAsync(storedToken);
        _userManagerMock.Setup(x => x.FindByIdAsync("1"))
            .ReturnsAsync(_testUser);
        _jwtTokenServiceMock.Setup(x => x.GenerateToken(
                _testUser.Id, _testUser.UserName!, It.IsAny<IEnumerable<string>>()))
            .Returns("new-access-token");
        _refreshTokenServiceMock.Setup(x => x.RevokeAsync(10))
            .ReturnsAsync(true);
        _refreshTokenServiceMock.Setup(x => x.CreateAsync(_testUser.Id))
            .ReturnsAsync(new RefreshToken { Token = "new-refresh-token" });

        var result = await _sut.HandleAsync(new RefreshRequest("valid-refresh-token"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().Be("new-access-token");
        result.Value.RefreshToken.Should().Be("new-refresh-token");
        result.Value.ExpiresIn.Should().Be(900);
    }

    [Fact]
    public async Task HandleAsync_revokes_old_token_when_issuing_new_tokens()
    {
        var storedToken = new RefreshToken { Id = 10, Token = "raw-token", UserId = 1, IsRevoked = false };
        _refreshTokenServiceMock.Setup(x => x.ValidateAsync("valid-refresh-token"))
            .ReturnsAsync(storedToken);
        _userManagerMock.Setup(x => x.FindByIdAsync("1"))
            .ReturnsAsync(_testUser);
        _jwtTokenServiceMock.Setup(x => x.GenerateToken(
                _testUser.Id, _testUser.UserName!, It.IsAny<IEnumerable<string>>()))
            .Returns("new-access-token");
        _refreshTokenServiceMock.Setup(x => x.RevokeAsync(10))
            .ReturnsAsync(true);
        _refreshTokenServiceMock.Setup(x => x.CreateAsync(_testUser.Id))
            .ReturnsAsync(new RefreshToken { Token = "new-refresh-token" });

        await _sut.HandleAsync(new RefreshRequest("valid-refresh-token"));

        _refreshTokenServiceMock.Verify(x => x.RevokeAsync(10), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_returns_failure_for_invalid_token()
    {
        _refreshTokenServiceMock.Setup(x => x.ValidateAsync("invalid-token"))
            .ReturnsAsync((RefreshToken?)null);

        var result = await _sut.HandleAsync(new RefreshRequest("invalid-token"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid or expired refresh token");
    }

    [Fact]
    public async Task HandleAsync_returns_failure_when_user_not_found()
    {
        var storedToken = new RefreshToken { Id = 10, Token = "raw-token", UserId = 999, IsRevoked = false };
        _refreshTokenServiceMock.Setup(x => x.ValidateAsync("token-for-missing-user"))
            .ReturnsAsync(storedToken);
        _userManagerMock.Setup(x => x.FindByIdAsync("999"))
            .ReturnsAsync((User?)null);

        var result = await _sut.HandleAsync(new RefreshRequest("token-for-missing-user"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User not found");
    }

    [Fact]
    public async Task HandleAsync_revokes_all_tokens_on_reuse_detection()
    {
        var revokedToken = new RefreshToken { Id = 10, Token = "raw-token", UserId = 1, IsRevoked = true };
        _refreshTokenServiceMock.Setup(x => x.ValidateAsync("reused-token"))
            .ReturnsAsync(revokedToken);

        var result = await _sut.HandleAsync(new RefreshRequest("reused-token"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid or expired refresh token");
        _refreshTokenServiceMock.Verify(x => x.RevokeAllForUserAsync(1), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_revokes_all_tokens_when_concurrent_revocation_detected()
    {
        var storedToken = new RefreshToken { Id = 10, Token = "raw-token", UserId = 1, IsRevoked = false };
        _refreshTokenServiceMock.Setup(x => x.ValidateAsync("concurrent-token"))
            .ReturnsAsync(storedToken);
        _userManagerMock.Setup(x => x.FindByIdAsync("1"))
            .ReturnsAsync(_testUser);
        _refreshTokenServiceMock.Setup(x => x.RevokeAsync(10))
            .ReturnsAsync(false); // Another request already revoked this token

        var result = await _sut.HandleAsync(new RefreshRequest("concurrent-token"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid or expired refresh token");
        _refreshTokenServiceMock.Verify(x => x.RevokeAllForUserAsync(1), Times.Once);
    }

    private static Mock<UserManager<User>> CreateUserManagerMock()
    {
        return new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(),
            Mock.Of<Microsoft.Extensions.Options.IOptions<IdentityOptions>>(),
            Mock.Of<IPasswordHasher<User>>(),
            Array.Empty<IUserValidator<User>>(),
            Array.Empty<IPasswordValidator<User>>(),
            Mock.Of<ILookupNormalizer>(),
            Mock.Of<IdentityErrorDescriber>(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<User>>>());
    }
}
