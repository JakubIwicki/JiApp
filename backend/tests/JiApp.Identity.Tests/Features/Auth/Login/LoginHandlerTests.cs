using JiApp.Common.Models;
using JiApp.Identity.Configuration;
using JiApp.Identity.Features.Auth.Login;
using JiApp.Identity.Models;
using JiApp.Identity.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Identity.Tests.Features.Auth.Login;

public class LoginHandlerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<SignInManager<User>> _signInManagerMock;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock;
    private readonly Mock<IUserModuleGrantService> _grantServiceMock;
    private readonly Mock<IPasswordHasher<User>> _passwordHasherMock;
    private readonly IdentitySettings _settings;
    private readonly User _testUser;
    private readonly LoginHandler _sut;

    public LoginHandlerTests()
    {
        _testUser = new User
        {
            Id = 1,
            UserName = "testuser",
            DisplayName = "Test User",
            Email = "test@test.com",
            SecurityStamp = "stamp",
            ConcurrencyStamp = "concurrency"
        };

        _userManagerMock = CreateUserManagerMock();
        _signInManagerMock = CreateSignInManagerMock(_userManagerMock.Object);

        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
        _jwtTokenServiceMock.Setup(x => x.GenerateToken(
                _testUser.Id, _testUser.UserName!, It.IsAny<IEnumerable<string>>()))
            .Returns("access-token");

        _refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        _refreshTokenServiceMock.Setup(x => x.CreateAsync(_testUser.Id))
            .ReturnsAsync(new RefreshToken { Token = "refresh-token", Id = 10 });

        _grantServiceMock = new Mock<IUserModuleGrantService>();
        _grantServiceMock.Setup(x => x.GetModulesAsync(_testUser.Id))
            .ReturnsAsync(["YtDownloader", "Scheduler"]);

        _passwordHasherMock = new Mock<IPasswordHasher<User>>();
        _passwordHasherMock.Setup(x => x.HashPassword(It.IsAny<User>(), It.IsAny<string>()))
            .Returns("dummy-hash");
        _passwordHasherMock.Setup(x => x.VerifyHashedPassword(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(PasswordVerificationResult.Failed);

        _settings = new IdentitySettings
        {
            Jwt = new IdentitySettings.JwtSettings { AccessTokenExpireMinutes = 15 }
        };

        var logger = Mock.Of<ILogger<LoginHandler>>();

        _sut = new LoginHandler(
            _signInManagerMock.Object,
            _userManagerMock.Object,
            _jwtTokenServiceMock.Object,
            _refreshTokenServiceMock.Object,
            _grantServiceMock.Object,
            _passwordHasherMock.Object,
            _settings,
            logger);
    }

    [Fact]
    public async Task HandleAsync_returns_success_with_tokens_for_valid_credentials()
    {
        _userManagerMock.Setup(x => x.FindByNameAsync("testuser"))
            .ReturnsAsync(_testUser);
        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(_testUser, "correct-password", true))
            .ReturnsAsync(SignInResult.Success);

        var result = await _sut.HandleAsync(new LoginRequest("testuser", "correct-password"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.UserId.Should().Be(_testUser.Id);
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        result.Value.ExpiresIn.Should().Be(900); // 15 min * 60
        result.Value.Modules.Should().BeEquivalentTo("YtDownloader", "Scheduler");
    }

    [Fact]
    public async Task HandleAsync_passes_granted_modules_into_access_token()
    {
        _userManagerMock.Setup(x => x.FindByNameAsync("testuser"))
            .ReturnsAsync(_testUser);
        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(_testUser, "correct-password", true))
            .ReturnsAsync(SignInResult.Success);

        await _sut.HandleAsync(new LoginRequest("testuser", "correct-password"));

        _jwtTokenServiceMock.Verify(x => x.GenerateToken(
            _testUser.Id,
            _testUser.UserName!,
            It.Is<IEnumerable<string>>(m => m.SequenceEqual(new[] { "YtDownloader", "Scheduler" }))),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_returns_failure_for_nonexistent_user()
    {
        _userManagerMock.Setup(x => x.FindByNameAsync("unknown"))
            .ReturnsAsync((User?)null);

        var result = await _sut.HandleAsync(new LoginRequest("unknown", "any-password"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid username or password");
    }

    [Fact]
    public async Task HandleAsync_calls_password_hasher_for_nonexistent_user_to_prevent_timing_attack()
    {
        _userManagerMock.Setup(x => x.FindByNameAsync("unknown"))
            .ReturnsAsync((User?)null);

        await _sut.HandleAsync(new LoginRequest("unknown", "any-password"));

        _passwordHasherMock.Verify(x => x.HashPassword(It.IsAny<User>(), It.IsAny<string>()), Times.Once);
        _passwordHasherMock.Verify(x => x.VerifyHashedPassword(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_returns_failure_for_wrong_password()
    {
        _userManagerMock.Setup(x => x.FindByNameAsync("testuser"))
            .ReturnsAsync(_testUser);
        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(_testUser, "wrong-password", true))
            .ReturnsAsync(SignInResult.Failed);

        var result = await _sut.HandleAsync(new LoginRequest("testuser", "wrong-password"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid username or password");
    }

    [Fact]
    public async Task HandleAsync_returns_lockout_message_when_account_is_locked()
    {
        _userManagerMock.Setup(x => x.FindByNameAsync("testuser"))
            .ReturnsAsync(_testUser);
        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(_testUser, "any-password", true))
            .ReturnsAsync(SignInResult.LockedOut);

        var result = await _sut.HandleAsync(new LoginRequest("testuser", "any-password"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Account is locked. Please try again later.");
    }

    private static Mock<SignInManager<User>> CreateSignInManagerMock(UserManager<User> userManager)
    {
        return new Mock<SignInManager<User>>(
            userManager,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<User>>(),
            Mock.Of<Microsoft.Extensions.Options.IOptions<IdentityOptions>>(),
            Mock.Of<ILogger<SignInManager<User>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<User>>());
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
