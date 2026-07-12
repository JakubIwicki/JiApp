using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Admin.Common;
using JiApp.Identity.Features.Admin.Users.RemoveRole;
using JiApp.Identity.Tests.Mocks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Identity.Tests.Features.Admin.Users.RemoveRole;

public sealed class RemoveRoleHandlerTests
{
	private sealed class Fixture
	{
		private readonly User _testUser = new()
		{
			Id = 2,
			UserName = "targetuser"
		};

		public MockUserManager UserManagerDouble { get; } = MockUserManager.GetSuccessful();
		public MockCurrentUserService CurrentUserMock { get; } = new();

		public AdminAccessGuard Guard { get; }
		public RemoveRoleHandler Sut { get; }

		public Fixture()
		{
			CurrentUserMock.WithReturning(1);
			Guard = new AdminAccessGuard(UserManagerDouble.Object, CurrentUserMock.Object);
			Sut = new RemoveRoleHandler(UserManagerDouble.Object, Guard, Mock.Of<ILogger<RemoveRoleHandler>>());
		}

		public Fixture WithTargetUserAndRole(string roleName)
		{
			UserManagerDouble.WithFindByIdAsync("2", _testUser);
			UserManagerDouble.WithRemoveFromRoleAsync(_testUser, roleName, IdentityResult.Success);
			UserManagerDouble.WithUpdateSecurityStampAsync(_testUser, IdentityResult.Success);
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
	public async Task HandleAsync_ReturnsSuccess_WhenRemovingNonAdminRole()
	{
		var fixture = new Fixture().WithTargetUserAndRole("User");

		var result = await fixture.Sut.HandleAsync(2, "User", CancellationToken.None);

		AssertSuccess(result);
		fixture.UserManagerDouble.VerifyUpdatedSecurityStamp_Once();
	}

	[Fact]
	public async Task HandleAsync_ReturnsAccessDenied_WhenRemovingAdminFromLastAdmin()
	{
		var fixture = new Fixture().WithTargetAsLastAdmin();

		var result = await fixture.Sut.HandleAsync(2, "Admin", CancellationToken.None);

		AssertAccessDenied(result);
		fixture.UserManagerDouble.VerifyUpdatedSecurityStamp_NotCalled();
	}

	[Fact]
	public async Task HandleAsync_ReturnsNotFound_WhenUserDoesNotExist()
	{
		var fixture = new Fixture();
		fixture.UserManagerDouble.WithFindByIdAsync("999", null);

		var result = await fixture.Sut.HandleAsync(999, "User", CancellationToken.None);

		AssertNotFound(result);
		fixture.UserManagerDouble.VerifyUpdatedSecurityStamp_NotCalled();
	}
}
