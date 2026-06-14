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
    private sealed class Fixture
    {
        private readonly User _testUser = new()
        {
            Id = 1,
            UserName = "testuser",
            DisplayName = "Test User",
            Email = "test@test.com",
            SecurityStamp = "stamp",
            ConcurrencyStamp = "concurrency"
        };

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

        public Mock<IHttpContextAccessor> HttpContextAccessorMock { get; } = new();

        public Mock<SignInManager<User>> SignInManagerMock { get; }
        public Mock<IJwtTokenService> JwtTokenServiceMock { get; } = new();
        public Mock<IRefreshTokenService> RefreshTokenServiceMock { get; } = new();
        public Mock<IUserModuleGrantService> GrantServiceMock { get; } = new();
        public Mock<IPasswordHasher<User>> PasswordHasherMock { get; } = new();
        public IdentitySettings Settings { get; } = new()
        {
            Jwt = new IdentitySettings.JwtSettings { AccessTokenExpireMinutes = 15 }
        };

        public Fixture()
        {
            SignInManagerMock = new Mock<SignInManager<User>>(
                UserManagerMock.Object,
                HttpContextAccessorMock.Object,
                Mock.Of<IUserClaimsPrincipalFactory<User>>(),
                Mock.Of<Microsoft.Extensions.Options.IOptions<IdentityOptions>>(),
                Mock.Of<ILogger<SignInManager<User>>>(),
                Mock.Of<IAuthenticationSchemeProvider>(),
                Mock.Of<IUserConfirmation<User>>());

            PasswordHasherMock
                .Setup(x => x.HashPassword(It.IsAny<User>(), It.IsAny<string>()))
                .Returns("dummy-hash");
            PasswordHasherMock
                .Setup(x => x.VerifyHashedPassword(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(PasswordVerificationResult.Failed);
        }

        public Fixture WithExistingUser()
        {
            UserManagerMock.Setup(x => x.FindByNameAsync("testuser"))
                .ReturnsAsync(_testUser);
            SignInManagerMock
                .Setup(x => x.CheckPasswordSignInAsync(_testUser, "correct-password", true))
                .ReturnsAsync(SignInResult.Success);
            JwtTokenServiceMock
                .Setup(x => x.GenerateToken(_testUser.Id, _testUser.UserName!, It.IsAny<IEnumerable<string>>()))
                .Returns("access-token");
            RefreshTokenServiceMock
                .Setup(x => x.CreateAsync(_testUser.Id))
                .ReturnsAsync(new RefreshToken { Token = "refresh-token", Id = 10 });
            GrantServiceMock
                .Setup(x => x.GetModulesAsync(_testUser.Id))
                .ReturnsAsync(["YtDownloader", "Scheduler"]);
            return this;
        }

        public Fixture WithNonexistentUser()
        {
            UserManagerMock.Setup(x => x.FindByNameAsync("unknown"))
                .ReturnsAsync((User?)null);
            return this;
        }

        public Fixture WithWrongPassword()
        {
            UserManagerMock.Setup(x => x.FindByNameAsync("testuser"))
                .ReturnsAsync(_testUser);
            SignInManagerMock
                .Setup(x => x.CheckPasswordSignInAsync(_testUser, "wrong-password", true))
                .ReturnsAsync(SignInResult.Failed);
            return this;
        }

        public Fixture WithLockedAccount()
        {
            UserManagerMock.Setup(x => x.FindByNameAsync("testuser"))
                .ReturnsAsync(_testUser);
            SignInManagerMock
                .Setup(x => x.CheckPasswordSignInAsync(_testUser, It.IsAny<string>(), true))
                .ReturnsAsync(SignInResult.LockedOut);
            return this;
        }

        public LoginHandler Build() =>
            new(
                SignInManagerMock.Object,
                UserManagerMock.Object,
                JwtTokenServiceMock.Object,
                RefreshTokenServiceMock.Object,
                GrantServiceMock.Object,
                PasswordHasherMock.Object,
                Settings,
                Mock.Of<ILogger<LoginHandler>>());
    }

    [Fact]
    public async Task HandleAsync_ReturnsSuccess_ForValidCredentials()
    {
        var fixture = new Fixture().WithExistingUser();
        var sut = fixture.Build();

        var result = await sut.HandleAsync(new LoginRequest("testuser", "correct-password"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.UserId.Should().Be(1);
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        result.Value.ExpiresIn.Should().Be(900);
        result.Value.Modules.Should().BeEquivalentTo("YtDownloader", "Scheduler");
    }

    [Fact]
    public async Task HandleAsync_PassesGrantedModules_IntoAccessToken()
    {
        var fixture = new Fixture().WithExistingUser();
        var sut = fixture.Build();

        await sut.HandleAsync(new LoginRequest("testuser", "correct-password"));

        fixture.JwtTokenServiceMock.Verify(x => x.GenerateToken(
            1, "testuser",
            It.Is<IEnumerable<string>>(m => m.SequenceEqual(new[] { "YtDownloader", "Scheduler" }))),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ReturnsFailure_ForNonexistentUser()
    {
        var fixture = new Fixture().WithNonexistentUser();
        var sut = fixture.Build();

        var result = await sut.HandleAsync(new LoginRequest("unknown", "any-password"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid username or password");
    }

    [Fact]
    public async Task HandleAsync_CallsPasswordHasher_ForNonexistentUser_ToPreventTimingAttack()
    {
        var fixture = new Fixture().WithNonexistentUser();
        var sut = fixture.Build();

        await sut.HandleAsync(new LoginRequest("unknown", "any-password"));

        fixture.PasswordHasherMock.Verify(
            x => x.HashPassword(It.IsAny<User>(), It.IsAny<string>()), Times.Once);
        fixture.PasswordHasherMock.Verify(
            x => x.VerifyHashedPassword(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ReturnsFailure_ForWrongPassword()
    {
        var fixture = new Fixture().WithWrongPassword();
        var sut = fixture.Build();

        var result = await sut.HandleAsync(new LoginRequest("testuser", "wrong-password"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid username or password");
    }

    [Fact]
    public async Task HandleAsync_ReturnsLockoutMessage_WhenAccountIsLocked()
    {
        var fixture = new Fixture().WithLockedAccount();
        var sut = fixture.Build();

        var result = await sut.HandleAsync(new LoginRequest("testuser", "any-password"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Account is locked. Please try again later.");
    }
}
