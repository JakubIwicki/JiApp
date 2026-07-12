using JiApp.Common;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Admin.Common;
using JiApp.Identity.Tests.Mocks;
using Microsoft.AspNetCore.Identity;

namespace JiApp.Identity.Tests.Features.Admin.Common;

public sealed class AdminAccessGuardTests
{
	private sealed class Fixture
	{
		private readonly User _adminA = new() { Id = 1, UserName = "adminA" };
		private readonly User _adminB = new() { Id = 2, UserName = "adminB" };

		public MockUserManager UserManagerDouble { get; } = MockUserManager.GetSuccessful();
		public MockCurrentUserService CurrentUserMock { get; } = new();

		public AdminAccessGuard Sut { get; }

		public Fixture()
		{
			CurrentUserMock.WithReturning(999);
			Sut = new AdminAccessGuard(UserManagerDouble.Object, CurrentUserMock.Object);
		}

		public Fixture WithTwoAdminsOneLockedOut(long targetUserId)
		{
			var target = targetUserId == _adminA.Id ? _adminA : _adminB;
			var other = target == _adminA ? _adminB : _adminA;

			UserManagerDouble.WithFindByIdAsync(targetUserId.ToString(), target);
			UserManagerDouble.WithIsInRoleAsync(target, RoleNames.Admin, true);
			UserManagerDouble.WithGetUsersInRoleAsync(RoleNames.Admin, [_adminA, _adminB]);
			UserManagerDouble.WithIsLockedOutAsync(target, false);
			UserManagerDouble.WithIsLockedOutAsync(other, true);
			return this;
		}

		public Fixture WithTwoAdminsOneLockedOutTargetDisabled(long targetUserId)
		{
			var target = targetUserId == _adminA.Id ? _adminA : _adminB;
			var other = target == _adminA ? _adminB : _adminA;

			UserManagerDouble.WithFindByIdAsync(targetUserId.ToString(), target);
			UserManagerDouble.WithIsInRoleAsync(target, RoleNames.Admin, true);
			UserManagerDouble.WithGetUsersInRoleAsync(RoleNames.Admin, [_adminA, _adminB]);
			UserManagerDouble.WithIsLockedOutAsync(target, true);
			UserManagerDouble.WithIsLockedOutAsync(other, false);
			return this;
		}

		public Fixture WithTwoActiveAdmins(long targetUserId)
		{
			var target = targetUserId == _adminA.Id ? _adminA : _adminB;

			UserManagerDouble.WithFindByIdAsync(targetUserId.ToString(), target);
			UserManagerDouble.WithIsInRoleAsync(target, RoleNames.Admin, true);
			UserManagerDouble.WithGetUsersInRoleAsync(RoleNames.Admin, [_adminA, _adminB]);
			UserManagerDouble.WithIsLockedOutAsync(_adminA, false);
			UserManagerDouble.WithIsLockedOutAsync(_adminB, false);
			return this;
		}
	}

	[Fact]
	public async Task EnsureNotLastAdminAsync_ReturnsAccessDenied_WhenOnlyOneEffectiveAdminRemains()
	{
		var fixture = new Fixture().WithTwoAdminsOneLockedOut(targetUserId: 1);

		var result = await fixture.Sut.EnsureNotLastAdminAsync(1);

		AssertAccessDenied(result);
	}

	[Fact]
	public async Task EnsureNotLastAdminAsync_ReturnsSuccess_WhenTargetIsAlreadyLockedOut()
	{
		var fixture = new Fixture().WithTwoAdminsOneLockedOutTargetDisabled(targetUserId: 2);

		var result = await fixture.Sut.EnsureNotLastAdminAsync(2);

		AssertSuccess(result);
	}

	[Fact]
	public async Task EnsureNotLastAdminAsync_ReturnsSuccess_WhenTwoEffectiveAdminsExist()
	{
		var fixture = new Fixture().WithTwoActiveAdmins(targetUserId: 1);

		var result = await fixture.Sut.EnsureNotLastAdminAsync(1);

		AssertSuccess(result);
	}

	[Fact]
	public async Task EnsureNotLastAdminAsync_ReturnsSuccess_WhenTargetIsNotAdmin()
	{
		var fixture = new Fixture();
		var regularUser = new User { Id = 5, UserName = "regular" };
		fixture.UserManagerDouble.WithFindByIdAsync("5", regularUser);
		fixture.UserManagerDouble.WithIsInRoleAsync(regularUser, RoleNames.Admin, false);

		var result = await fixture.Sut.EnsureNotLastAdminAsync(5);

		AssertSuccess(result);
	}

	[Fact]
	public void EnsureNotSelf_ReturnsAccessDenied_WhenTargetIsSelf()
	{
		var fixture = new Fixture();
		fixture.CurrentUserMock.WithReturning(42);

		var result = fixture.Sut.EnsureNotSelf(42);

		AssertAccessDenied(result);
	}

	[Fact]
	public void EnsureNotSelf_ReturnsSuccess_WhenTargetIsOther()
	{
		var fixture = new Fixture();
		fixture.CurrentUserMock.WithReturning(42);

		var result = fixture.Sut.EnsureNotSelf(99);

		AssertSuccess(result);
	}
}
