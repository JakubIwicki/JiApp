using JiApp.Common.Abstractions;
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

public sealed class LoginHandlerTests
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
        public Mock<IUserAccessService> AccessServiceMock { get; } = new();
        public Mock<IPasswordHasher<User>> PasswordHasherMock { get; } = new();
        public IdentitySettings Settings { get; } = new()
        {
            Jwt = new IdentitySettings.JwtSettings { AccessTokenExpireMinutes = 15 }
        };

        public LoginHandler Sut { get; }

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

            Sut = new LoginHandler(
                SignInManagerMock.Object,
                UserManagerMock.Object,
                JwtTokenServiceMock.Object,
                RefreshTokenServiceMock.Object,
                AccessServiceMock.Object,
                PasswordHasherMock.Object,
                Settings,
                Mock.Of<ILogger<LoginHandler>>());
        }

        public Fixture WithExistingUser()
        {
            UserManagerMock.Setup(x => x.FindByNameAsync("testuser"))
                .ReturnsAsync(_testUser);
            SignInManagerMock
                .Setup(x => x.CheckPasswordSignInAsync(_testUser, "correct-password", true))
                .ReturnsAsync(SignInResult.Success);
            JwtTokenServiceMock
                .Setup(x => x.GenerateToken(_testUser.Id, _testUser.UserName!, It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>()))
                .Returns("access-token");
            RefreshTokenServiceMock
                .Setup(x => x.CreateAsync(_testUser.Id))
                .ReturnsAsync(new RefreshToken { Token = "refresh-token", Id = 10 });
            UserManagerMock
                .Setup(x => x.GetRolesAsync(_testUser))
                .ReturnsAsync(["User"]);
            AccessServiceMock
                .Setup(x => x.GetEffectivePermissionsAsync(_testUser.Id))
                .ReturnsAsync(["ytdownloader.access", "scheduler.access"]);
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
    }

    [Fact]
    public async Task HandleAsync_ReturnsSuccess_ForValidCredentials()
    {
        var fixture = new Fixture().WithExistingUser();

        var result = await fixture.Sut.HandleAsync(new LoginRequest("testuser", "correct-password"));

        AssertSuccess(result);
        result.Value.Should().NotBeNull();
        result.Value!.UserId.Should().Be(1);
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        result.Value.ExpiresIn.Should().Be(900);
        result.Value.Roles.Should().BeEquivalentTo("User");
        result.Value.Permissions.Should().BeEquivalentTo("ytdownloader.access", "scheduler.access");
    }

    [Fact]
    public async Task HandleAsync_PassesRolesAndPermissions_IntoAccessToken()
    {
        var fixture = new Fixture().WithExistingUser();

        await fixture.Sut.HandleAsync(new LoginRequest("testuser", "correct-password"));

        fixture.JwtTokenServiceMock.Verify(x => x.GenerateToken(
            1, "testuser",
            It.Is<IEnumerable<string>>(r => r.SequenceEqual(new[] { "User" })),
            It.Is<IEnumerable<string>>(p => p.SequenceEqual(new[] { "ytdownloader.access", "scheduler.access" })),
            "stamp"),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ReturnsFailure_ForNonexistentUser()
    {
        var fixture = new Fixture().WithNonexistentUser();

        var result = await fixture.Sut.HandleAsync(new LoginRequest("unknown", "any-password"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid username or password");
    }

    [Fact]
    public async Task HandleAsync_CallsPasswordHasher_ForNonexistentUser_ToPreventTimingAttack()
    {
        var fixture = new Fixture().WithNonexistentUser();

        await fixture.Sut.HandleAsync(new LoginRequest("unknown", "any-password"));

        fixture.PasswordHasherMock.Verify(
            x => x.HashPassword(It.IsAny<User>(), It.IsAny<string>()), Times.Once);
        fixture.PasswordHasherMock.Verify(
            x => x.VerifyHashedPassword(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ReturnsFailure_ForWrongPassword()
    {
        var fixture = new Fixture().WithWrongPassword();

        var result = await fixture.Sut.HandleAsync(new LoginRequest("testuser", "wrong-password"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid username or password");
    }

    [Fact]
    public async Task HandleAsync_ReturnsLockout_WhenAccountIsLocked()
    {
        var fixture = new Fixture().WithLockedAccount();

        var result = await fixture.Sut.HandleAsync(new LoginRequest("testuser", "any-password"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Account is locked. Please try again later.");
    }

    [Fact]
    public async Task HandleAsync_PopulatesSecurityStamp_WhenNull_AndGeneratesToken()
    {
        var fixture = new Fixture();
        var userWithNullStamp = new User
        {
            Id = 1,
            UserName = "testuser",
            DisplayName = "Test User",
            SecurityStamp = null
        };
        fixture.UserManagerMock.Setup(x => x.FindByNameAsync("testuser"))
            .ReturnsAsync(userWithNullStamp);
        fixture.SignInManagerMock
            .Setup(x => x.CheckPasswordSignInAsync(userWithNullStamp, "pass", true))
            .ReturnsAsync(SignInResult.Success);
        fixture.UserManagerMock
            .Setup(x => x.UpdateSecurityStampAsync(userWithNullStamp))
            .Callback<User>(u => u.SecurityStamp = "generated-stamp")
            .ReturnsAsync(IdentityResult.Success);
        fixture.JwtTokenServiceMock
            .Setup(x => x.GenerateToken(1, "testuser", It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), "generated-stamp"))
            .Returns("access-token");
        fixture.RefreshTokenServiceMock
            .Setup(x => x.CreateAsync(1))
            .ReturnsAsync(new RefreshToken { Token = "refresh-token", Id = 10 });
        fixture.UserManagerMock
            .Setup(x => x.GetRolesAsync(userWithNullStamp))
            .ReturnsAsync(["User"]);
        fixture.AccessServiceMock
            .Setup(x => x.GetEffectivePermissionsAsync(1))
            .ReturnsAsync(["ytdownloader.access"]);

        var result = await fixture.Sut.HandleAsync(new LoginRequest("testuser", "pass"));

        AssertSuccess(result);
        fixture.UserManagerMock.Verify(x => x.UpdateSecurityStampAsync(userWithNullStamp), Times.Once);
        fixture.JwtTokenServiceMock.Verify(x => x.GenerateToken(1, "testuser",
            It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), "generated-stamp"), Times.Once);
    }
}
