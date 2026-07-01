using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Admin.Common;
using JiApp.Identity.Features.Admin.Users.DisableUser;
using JiApp.Identity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Identity.Tests.Features.Admin.Users.DisableUser;

public sealed class DisableUserHandlerTests
{
	private sealed class Fixture
	{
		private readonly User _testUser = new()
		{
			Id = 2,
			UserName = "targetuser",
			Email = "target@test.com"
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

		public Mock<ICurrentUserService> CurrentUserMock { get; } = new();
		public Mock<IRefreshTokenService> RefreshTokenServiceMock { get; } = new();

		public AdminAccessGuard Guard { get; }
		public DisableUserHandler Sut { get; }

		public Fixture()
		{
			CurrentUserMock.Setup(x => x.UserId).Returns(1);
			Guard = new AdminAccessGuard(UserManagerMock.Object, CurrentUserMock.Object);
			Sut = new DisableUserHandler(UserManagerMock.Object, RefreshTokenServiceMock.Object, Guard,
				Mock.Of<ILogger<DisableUserHandler>>());
		}

		public Fixture WithTargetUser()
		{
			UserManagerMock.Setup(x => x.FindByIdAsync("2"))
				.ReturnsAsync(_testUser);
			UserManagerMock.Setup(x => x.SetLockoutEnabledAsync(_testUser, true))
				.ReturnsAsync(IdentityResult.Success);
			UserManagerMock.Setup(x => x.SetLockoutEndDateAsync(_testUser, DateTimeOffset.MaxValue))
				.ReturnsAsync(IdentityResult.Success);
			UserManagerMock.Setup(x => x.UpdateSecurityStampAsync(_testUser))
				.ReturnsAsync(IdentityResult.Success);
			RefreshTokenServiceMock.Setup(x => x.RevokeAllForUserAsync(2))
				.Returns(Task.CompletedTask);
			return this;
		}

		public Fixture WithTargetAsSelf()
		{
			CurrentUserMock.Setup(x => x.UserId).Returns(2);
			return this;
		}

		public Fixture WithTargetAsLastAdmin()
		{
			UserManagerMock.Setup(x => x.FindByIdAsync("2"))
				.ReturnsAsync(_testUser);
			UserManagerMock.Setup(x => x.IsInRoleAsync(_testUser, "Admin"))
				.ReturnsAsync(true);
			UserManagerMock.Setup(x => x.GetUsersInRoleAsync("Admin"))
				.ReturnsAsync([_testUser]);
			return this;
		}
	}

	[Fact]
	public async Task HandleAsync_ReturnsSuccess_WhenDisablingAnotherUser()
	{
		var fixture = new Fixture().WithTargetUser();

		var result = await fixture.Sut.HandleAsync(2);

		AssertSuccess(result);
		fixture.UserManagerMock.Verify(x => x.SetLockoutEnabledAsync(It.IsAny<User>(), true), Times.Once);
		fixture.UserManagerMock.Verify(x => x.SetLockoutEndDateAsync(It.IsAny<User>(), DateTimeOffset.MaxValue), Times.Once);
		fixture.UserManagerMock.Verify(x => x.UpdateSecurityStampAsync(It.IsAny<User>()), Times.Once);
		fixture.RefreshTokenServiceMock.Verify(x => x.RevokeAllForUserAsync(2), Times.Once);
	}

	[Fact]
	public async Task HandleAsync_SetsPermanentLockout_WhenUserIsAlreadyTransientlyLockedOut()
	{
		var fixture = new Fixture();
		fixture.UserManagerMock.Setup(x => x.FindByIdAsync("2"))
			.ReturnsAsync(new User { Id = 2, UserName = "lockeduser" });
		fixture.UserManagerMock.Setup(x => x.IsLockedOutAsync(It.IsAny<User>()))
			.ReturnsAsync(true);
		fixture.UserManagerMock.Setup(x => x.SetLockoutEnabledAsync(It.IsAny<User>(), true))
			.ReturnsAsync(IdentityResult.Success);
		fixture.UserManagerMock.Setup(x => x.SetLockoutEndDateAsync(It.IsAny<User>(), DateTimeOffset.MaxValue))
			.ReturnsAsync(IdentityResult.Success);
		fixture.UserManagerMock.Setup(x => x.UpdateSecurityStampAsync(It.IsAny<User>()))
			.ReturnsAsync(IdentityResult.Success);
		fixture.RefreshTokenServiceMock.Setup(x => x.RevokeAllForUserAsync(2))
			.Returns(Task.CompletedTask);

		var result = await fixture.Sut.HandleAsync(2);

		AssertSuccess(result);
		fixture.UserManagerMock.Verify(x => x.SetLockoutEnabledAsync(It.IsAny<User>(), true), Times.Once);
		fixture.UserManagerMock.Verify(x => x.SetLockoutEndDateAsync(It.IsAny<User>(), DateTimeOffset.MaxValue), Times.Once);
		fixture.UserManagerMock.Verify(x => x.UpdateSecurityStampAsync(It.IsAny<User>()), Times.Once);
	}

	[Fact]
	public async Task HandleAsync_ReturnsAccessDenied_WhenSelfDisable()
	{
		var fixture = new Fixture().WithTargetAsSelf();

		var result = await fixture.Sut.HandleAsync(2);

		AssertAccessDenied(result);
		fixture.UserManagerMock.Verify(x => x.UpdateSecurityStampAsync(It.IsAny<User>()), Times.Never);
	}

	[Fact]
	public async Task HandleAsync_ReturnsAccessDenied_WhenDisablingLastAdmin()
	{
		var fixture = new Fixture().WithTargetAsLastAdmin();

		var result = await fixture.Sut.HandleAsync(2);

		AssertAccessDenied(result);
		fixture.UserManagerMock.Verify(x => x.UpdateSecurityStampAsync(It.IsAny<User>()), Times.Never);
	}

	[Fact]
	public async Task HandleAsync_ReturnsNotFound_WhenUserDoesNotExist()
	{
		var fixture = new Fixture();
		fixture.UserManagerMock.Setup(x => x.FindByIdAsync("999"))
			.ReturnsAsync((User?)null);

		var result = await fixture.Sut.HandleAsync(999);

		AssertNotFound(result);
		fixture.UserManagerMock.Verify(x => x.UpdateSecurityStampAsync(It.IsAny<User>()), Times.Never);
	}
}
