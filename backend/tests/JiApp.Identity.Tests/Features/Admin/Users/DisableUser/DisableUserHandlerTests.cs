using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Admin.Common;
using JiApp.Identity.Features.Admin.Users.DisableUser;
using JiApp.Identity.Tests.Mocks;
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

		public MockUserManager UserManagerDouble { get; } = MockUserManager.GetSuccessful();
		public MockCurrentUserService CurrentUserMock { get; } = new();
		public MockRefreshTokenService RefreshTokenDouble { get; } = MockRefreshTokenService.GetSuccessful();

		public AdminAccessGuard Guard { get; }
		public DisableUserHandler Sut { get; }

		public Fixture()
		{
			CurrentUserMock.WithReturning(1);
			Guard = new AdminAccessGuard(UserManagerDouble, CurrentUserMock.Object);
			Sut = new DisableUserHandler(UserManagerDouble, RefreshTokenDouble.Object, Guard,
				Mock.Of<ILogger<DisableUserHandler>>());
		}

		public Fixture WithTargetUser()
		{
			UserManagerDouble.WithFindByIdAsync("2", _testUser);
			UserManagerDouble.WithSetLockoutEnabledAsync(_testUser, true, IdentityResult.Success);
			UserManagerDouble.WithSetLockoutEndDateAsync(_testUser, DateTimeOffset.MaxValue, IdentityResult.Success);
			UserManagerDouble.WithUpdateSecurityStampAsync(_testUser, IdentityResult.Success);
			RefreshTokenDouble.WithRevokeAllForUserAsync(2);
			return this;
		}

		public Fixture WithTargetAsSelf()
		{
			CurrentUserMock.WithReturning(2);
			return this;
		}

		public Fixture WithTargetAsLastAdmin()
		{
			UserManagerDouble.WithFindByIdAsync("2", _testUser);
			UserManagerDouble.WithIsInRoleAsync(_testUser, "Admin", true);
			UserManagerDouble.WithGetUsersInRoleAsync("Admin", [_testUser]);
			return this;
		}
	}

	[Fact]
	public async Task HandleAsync_ReturnsSuccess_WhenDisablingAnotherUser()
	{
		var fixture = new Fixture().WithTargetUser();

		var result = await fixture.Sut.HandleAsync(2, CancellationToken.None);

		AssertSuccess(result);
		fixture.UserManagerDouble.VerifySetLockoutEnabled();
		fixture.UserManagerDouble.VerifySetLockoutEndDatePermanent();
		fixture.UserManagerDouble.VerifyUpdatedSecurityStamp_Once();
		fixture.RefreshTokenDouble.VerifyRevokedAllForUser(2);
	}

	[Fact]
	public async Task HandleAsync_SetsPermanentLockout_WhenUserIsAlreadyTransientlyLockedOut()
	{
		var fixture = new Fixture();
		var lockedUser = new User { Id = 2, UserName = "lockeduser" };
		fixture.UserManagerDouble.WithFindByIdAsync("2", lockedUser);
		fixture.UserManagerDouble.WithIsLockedOutAsync(lockedUser, true);
		fixture.UserManagerDouble.WithSetLockoutEnabledAsync(lockedUser, true, IdentityResult.Success);
		fixture.UserManagerDouble.WithSetLockoutEndDateAsync(lockedUser, DateTimeOffset.MaxValue, IdentityResult.Success);
		fixture.UserManagerDouble.WithUpdateSecurityStampAsync(lockedUser, IdentityResult.Success);
		fixture.RefreshTokenDouble.WithRevokeAllForUserAsync(2);

		var result = await fixture.Sut.HandleAsync(2, CancellationToken.None);

		AssertSuccess(result);
		fixture.UserManagerDouble.VerifySetLockoutEnabled();
		fixture.UserManagerDouble.VerifySetLockoutEndDatePermanent();
		fixture.UserManagerDouble.VerifyUpdatedSecurityStamp_Once();
	}

	[Fact]
	public async Task HandleAsync_ReturnsAccessDenied_WhenSelfDisable()
	{
		var fixture = new Fixture().WithTargetAsSelf();

		var result = await fixture.Sut.HandleAsync(2, CancellationToken.None);

		AssertAccessDenied(result);
		fixture.UserManagerDouble.VerifyUpdatedSecurityStamp_NotCalled();
	}

	[Fact]
	public async Task HandleAsync_ReturnsAccessDenied_WhenDisablingLastAdmin()
	{
		var fixture = new Fixture().WithTargetAsLastAdmin();

		var result = await fixture.Sut.HandleAsync(2, CancellationToken.None);

		AssertAccessDenied(result);
		fixture.UserManagerDouble.VerifyUpdatedSecurityStamp_NotCalled();
	}

	[Fact]
	public async Task HandleAsync_ReturnsNotFound_WhenUserDoesNotExist()
	{
		var fixture = new Fixture();
		fixture.UserManagerDouble.WithFindByIdAsync("999", null);

		var result = await fixture.Sut.HandleAsync(999, CancellationToken.None);

		AssertNotFound(result);
		fixture.UserManagerDouble.VerifyUpdatedSecurityStamp_NotCalled();
	}
}
