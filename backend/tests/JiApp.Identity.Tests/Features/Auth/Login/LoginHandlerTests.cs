using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Configuration;
using JiApp.Identity.Features.Auth.Login;
using JiApp.Identity.Models;
using JiApp.Identity.Tests.Mocks;
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

        public MockUserManager UserManagerDouble { get; } = MockUserManager.GetSuccessful();
        public Mock<IHttpContextAccessor> HttpContextAccessorMock { get; } = new();
        public MockSignInManager SignInManagerDouble { get; }
        public MockJwtTokenService JwtTokenDouble { get; } = MockJwtTokenService.GetSuccessful();
        public MockRefreshTokenService RefreshTokenDouble { get; } = MockRefreshTokenService.GetSuccessful();
        public MockUserAccessService AccessServiceDouble { get; } = MockUserAccessService.GetSuccessful();
        public Mock<IPasswordHasher<User>> PasswordHasherMock { get; } = new();
        public IdentitySettings Settings { get; } = new()
        {
            Jwt = new IdentitySettings.JwtSettings { AccessTokenExpireMinutes = 15 }
        };

        public LoginHandler Sut { get; }

        public Fixture()
        {
            SignInManagerDouble = MockSignInManager.GetSuccessful(
                UserManagerDouble, HttpContextAccessorMock.Object);

            PasswordHasherMock
                .Setup(x => x.HashPassword(It.IsAny<User>(), It.IsAny<string>()))
                .Returns("dummy-hash");
            PasswordHasherMock
                .Setup(x => x.VerifyHashedPassword(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(PasswordVerificationResult.Failed);

            Sut = new LoginHandler(
                SignInManagerDouble,
                UserManagerDouble,
                JwtTokenDouble.Object,
                RefreshTokenDouble.Object,
                AccessServiceDouble.Object,
                PasswordHasherMock.Object,
                Settings,
                Mock.Of<ILogger<LoginHandler>>());
        }

        public Fixture WithExistingUser()
        {
            UserManagerDouble.WithFindByNameAsync("testuser", _testUser);
            SignInManagerDouble.WithCheckPasswordSignInAsyncSuccess(_testUser, "correct-password");
            JwtTokenDouble.WithGenerateTokenAny("access-token");
            RefreshTokenDouble.WithCreateAsync(_testUser.Id, new RefreshToken { Token = "refresh-token", Id = 10 });
            UserManagerDouble.WithGetRolesAsync(_testUser, ["User"]);
            AccessServiceDouble.WithGetEffectivePermissionsAsync(_testUser.Id,
                ["ytdownloader.access", "scheduler.access"]);
            return this;
        }

        public Fixture WithNonexistentUser()
        {
            UserManagerDouble.WithFindByNameAsync("unknown", null);
            return this;
        }

        public Fixture WithWrongPassword()
        {
            UserManagerDouble.WithFindByNameAsync("testuser", _testUser);
            SignInManagerDouble.WithCheckPasswordSignInAsyncFailed(_testUser, "wrong-password");
            return this;
        }

        public Fixture WithLockedAccount()
        {
            UserManagerDouble.WithFindByNameAsync("testuser", _testUser);
            SignInManagerDouble.WithCheckPasswordSignInAsyncLockedOut(_testUser);
            return this;
        }
    }

    [Fact]
    public async Task HandleAsync_ReturnsSuccess_ForValidCredentials()
    {
        var fixture = new Fixture().WithExistingUser();

        var result = await fixture.Sut.HandleAsync(new LoginRequest("testuser", "correct-password"), CancellationToken.None);

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

        await fixture.Sut.HandleAsync(new LoginRequest("testuser", "correct-password"), CancellationToken.None);

        fixture.JwtTokenDouble.VerifyGeneratedTokenWithRolesAndPermissions(
            1, "testuser",
            ["User"], ["ytdownloader.access", "scheduler.access"],
            "stamp");
    }

    [Fact]
    public async Task HandleAsync_ReturnsFailure_ForNonexistentUser()
    {
        var fixture = new Fixture().WithNonexistentUser();

        var result = await fixture.Sut.HandleAsync(new LoginRequest("unknown", "any-password"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid username or password");
    }

    [Fact]
    public async Task HandleAsync_CallsPasswordHasher_ForNonexistentUser_ToPreventTimingAttack()
    {
        var fixture = new Fixture().WithNonexistentUser();

        await fixture.Sut.HandleAsync(new LoginRequest("unknown", "any-password"), CancellationToken.None);

        fixture.PasswordHasherMock.Verify(
            x => x.HashPassword(It.IsAny<User>(), It.IsAny<string>()), Times.Once);
        fixture.PasswordHasherMock.Verify(
            x => x.VerifyHashedPassword(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ReturnsFailure_ForWrongPassword()
    {
        var fixture = new Fixture().WithWrongPassword();

        var result = await fixture.Sut.HandleAsync(new LoginRequest("testuser", "wrong-password"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid username or password");
    }

    [Fact]
    public async Task HandleAsync_ReturnsLockout_WhenAccountIsLocked()
    {
        var fixture = new Fixture().WithLockedAccount();

        var result = await fixture.Sut.HandleAsync(new LoginRequest("testuser", "any-password"), CancellationToken.None);

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
        fixture.UserManagerDouble.WithFindByNameAsync("testuser", userWithNullStamp);
        fixture.SignInManagerDouble.WithCheckPasswordSignInAsyncSuccess(userWithNullStamp, "pass");
        fixture.UserManagerDouble.WithUpdateSecurityStampAsync(userWithNullStamp, IdentityResult.Success,
            callback: u => u.SecurityStamp = "generated-stamp");
        fixture.JwtTokenDouble.WithGenerateToken(1, "testuser",
            It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), "generated-stamp",
            "access-token");
        fixture.RefreshTokenDouble.WithCreateAsync(1, new RefreshToken { Token = "refresh-token", Id = 10 });
        fixture.UserManagerDouble.WithGetRolesAsync(userWithNullStamp, ["User"]);
        fixture.AccessServiceDouble.WithGetEffectivePermissionsAsync(1, ["ytdownloader.access"]);

        var result = await fixture.Sut.HandleAsync(new LoginRequest("testuser", "pass"), CancellationToken.None);

        AssertSuccess(result);
        fixture.UserManagerDouble.VerifyUpdatedSecurityStamp(userWithNullStamp);
        fixture.JwtTokenDouble.VerifyGeneratedToken(1, "testuser", "generated-stamp");
    }
}
