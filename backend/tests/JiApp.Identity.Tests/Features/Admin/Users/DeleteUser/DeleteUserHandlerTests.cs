using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Admin.Common;
using JiApp.Identity.Features.Admin.Users.DeleteUser;
using JiApp.Identity.Tests.Mocks;
using Microsoft.AspNetCore.Identity;

namespace JiApp.Identity.Tests.Features.Admin.Users.DeleteUser;

public sealed class DeleteUserHandlerTests
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

		public AdminAccessGuard Guard { get; }
		public DeleteUserHandler Sut { get; }

		public Fixture()
		{
			CurrentUserMock.WithReturning(1);
			Guard = new AdminAccessGuard(UserManagerDouble.Object, CurrentUserMock.Object);
			Sut = new DeleteUserHandler(UserManagerDouble.Object, Guard);
		}

		public Fixture WithTargetUser()
		{
			UserManagerDouble.WithFindByIdAsync("2", _testUser);
			UserManagerDouble.WithIsInRoleAsync(_testUser, "Admin", false);
			UserManagerDouble.WithDeleteAsync(_testUser, IdentityResult.Success);
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
	public async Task HandleAsync_ReturnsSuccess_WhenDeletingAnotherUser()
	{
		var fixture = new Fixture().WithTargetUser();

		var result = await fixture.Sut.HandleAsync(2, CancellationToken.None);

		AssertSuccess(result);
		fixture.UserManagerDouble.VerifyDeleteAsync_Once();
	}

	[Fact]
	public async Task HandleAsync_ReturnsAccessDenied_WhenSelfDelete()
	{
		var fixture = new Fixture().WithTargetAsSelf();

		var result = await fixture.Sut.HandleAsync(2, CancellationToken.None);

		AssertAccessDenied(result);
	}

	[Fact]
	public async Task HandleAsync_ReturnsAccessDenied_WhenDeletingLastAdmin()
	{
		var fixture = new Fixture().WithTargetAsLastAdmin();

		var result = await fixture.Sut.HandleAsync(2, CancellationToken.None);

		AssertAccessDenied(result);
	}

	[Fact]
	public async Task HandleAsync_ReturnsNotFound_WhenUserDoesNotExist()
	{
		var fixture = new Fixture();
		fixture.UserManagerDouble.WithFindByIdAsync("999", null);

		var result = await fixture.Sut.HandleAsync(999, CancellationToken.None);

		AssertNotFound(result);
	}
}
