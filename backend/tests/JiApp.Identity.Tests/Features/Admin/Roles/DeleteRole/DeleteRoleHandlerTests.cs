using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Admin.Common;
using JiApp.Identity.Features.Admin.Roles.DeleteRole;
using JiApp.Identity.Tests.Mocks;
using Microsoft.AspNetCore.Identity;

namespace JiApp.Identity.Tests.Features.Admin.Roles.DeleteRole;

public sealed class DeleteRoleHandlerTests
{
	private sealed class Fixture
	{
		private readonly IdentityRole<long> _role = new("Moderator") { Id = 3 };

		public MockRoleManager RoleManagerDouble { get; } = MockRoleManager.GetSuccessful();
		public MockUserManager UserManagerDouble { get; } = MockUserManager.GetSuccessful();
		public MockCurrentUserService CurrentUserMock { get; } = new();

		public AdminAccessGuard Guard { get; }
		public DeleteRoleHandler Sut { get; }

		public Fixture()
		{
			CurrentUserMock.WithReturning(1);
			Guard = new AdminAccessGuard(UserManagerDouble.Object, CurrentUserMock.Object);
			Sut = new DeleteRoleHandler(RoleManagerDouble.Object, UserManagerDouble.Object, Guard);
		}

		public Fixture WithDeletableRole()
		{
			RoleManagerDouble.WithFindByNameAsync("Moderator", _role);
			UserManagerDouble.WithGetUsersInRoleAsync("Moderator", []);
			RoleManagerDouble.WithDeleteAsync(_role, IdentityResult.Success);
			return this;
		}

		public Fixture WithRoleHavingUsers()
		{
			RoleManagerDouble.WithFindByNameAsync("Moderator", _role);
			UserManagerDouble.WithGetUsersInRoleAsync("Moderator", [new User { Id = 1, UserName = "someuser" }]);
			return this;
		}
	}

	[Fact]
	public async Task HandleAsync_ReturnsSuccess_WhenDeletingCustomRoleWithNoUsers()
	{
		var fixture = new Fixture().WithDeletableRole();

		var result = await fixture.Sut.HandleAsync("Moderator", CancellationToken.None);

		AssertSuccess(result);
	}

	[Fact]
	public async Task HandleAsync_ReturnsAccessDenied_WhenDeletingReservedRole()
	{
		var fixture = new Fixture();

		var result = await fixture.Sut.HandleAsync("Admin", CancellationToken.None);

		AssertAccessDenied(result);
	}

	[Fact]
	public async Task HandleAsync_ReturnsAccessDenied_WhenDeletingGuestRole()
	{
		var fixture = new Fixture();

		var result = await fixture.Sut.HandleAsync("Guest", CancellationToken.None);

		AssertAccessDenied(result);
	}

	[Fact]
	public async Task HandleAsync_ReturnsConflict_WhenRoleHasUsers()
	{
		var fixture = new Fixture().WithRoleHavingUsers();

		var result = await fixture.Sut.HandleAsync("Moderator", CancellationToken.None);

		AssertConflict(result);
	}

	[Fact]
	public async Task HandleAsync_ReturnsNotFound_WhenRoleDoesNotExist()
	{
		var fixture = new Fixture();
		fixture.RoleManagerDouble.WithFindByNameAsync("FakeRole", null);

		var result = await fixture.Sut.HandleAsync("FakeRole", CancellationToken.None);

		AssertNotFound(result);
	}
}
