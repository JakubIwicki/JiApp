using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Admin.Common;
using JiApp.Identity.Features.Admin.Users.AssignRole;
using JiApp.Identity.Tests.Mocks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Identity.Tests.Features.Admin.Users.AssignRole;

public sealed class AssignRoleHandlerTests
{
	private sealed class Fixture
	{
		private readonly User _testUser = new()
		{
			Id = 1,
			UserName = "testuser"
		};

		public MockUserManager UserManagerDouble { get; } = MockUserManager.GetSuccessful();
		public MockRoleManager RoleManagerDouble { get; } = MockRoleManager.GetSuccessful();
		public MockCurrentUserService CurrentUserMock { get; } = new();

		public AdminAccessGuard Guard { get; }
		public AssignRoleHandler Sut { get; }

		public Fixture()
		{
			CurrentUserMock.WithReturning(999);
			Guard = new AdminAccessGuard(UserManagerDouble.Object, CurrentUserMock.Object);
			Sut = new AssignRoleHandler(UserManagerDouble.Object, RoleManagerDouble.Object, Guard,
				Mock.Of<ILogger<AssignRoleHandler>>());
		}

		public Fixture WithExistingUserAndRole()
		{
			UserManagerDouble.WithFindByIdAsync("1", _testUser);
			RoleManagerDouble.WithRoleExistsAsync("Admin", true);
			UserManagerDouble.WithAddToRoleAsync(_testUser, "Admin", IdentityResult.Success);
			UserManagerDouble.WithUpdateSecurityStampAsync(_testUser, IdentityResult.Success);
			return this;
		}

		public Fixture WithNonexistentRole()
		{
			RoleManagerDouble.WithRoleExistsAsync("FakeRole", false);
			return this;
		}

		public Fixture WithSelfTarget()
		{
			CurrentUserMock.WithReturning(1);
			return this;
		}
	}

	[Fact]
	public async Task HandleAsync_ReturnsSuccess_WhenAssigningValidRole()
	{
		var fixture = new Fixture().WithExistingUserAndRole();

		var result = await fixture.Sut.HandleAsync(1, new AssignRoleRequest("Admin"), CancellationToken.None);

		AssertSuccess(result);
		fixture.UserManagerDouble.VerifyUpdatedSecurityStamp_Once();
	}

	[Fact]
	public async Task HandleAsync_ReturnsValidationFailure_WhenRoleDoesNotExist()
	{
		var fixture = new Fixture().WithNonexistentRole();

		var result = await fixture.Sut.HandleAsync(1, new AssignRoleRequest("FakeRole"), CancellationToken.None);

		AssertValidationFailure(result);
		fixture.UserManagerDouble.VerifyUpdatedSecurityStamp_NotCalled();
	}

	[Fact]
	public async Task HandleAsync_ReturnsNotFound_WhenUserDoesNotExist()
	{
		var fixture = new Fixture();
		fixture.UserManagerDouble.WithFindByIdAsync("9999", null);
		fixture.RoleManagerDouble.WithRoleExistsAsync("Admin", true);

		var result = await fixture.Sut.HandleAsync(9999, new AssignRoleRequest("Admin"), CancellationToken.None);

		AssertNotFound(result);
		fixture.UserManagerDouble.VerifyUpdatedSecurityStamp_NotCalled();
	}

	[Fact]
	public async Task HandleAsync_ReturnsAccessDenied_WhenSelfAssigningAdminRole()
	{
		var fixture = new Fixture().WithSelfTarget();

		var result = await fixture.Sut.HandleAsync(1, new AssignRoleRequest("Admin"), CancellationToken.None);

		AssertAccessDenied(result);
		fixture.UserManagerDouble.VerifyUpdatedSecurityStamp_NotCalled();
	}

	[Fact]
	public async Task HandleAsync_ReturnsSuccess_WhenSelfAssigningNonAdminRole()
	{
		var fixture = new Fixture().WithSelfTarget();
		var selfUser = new User { Id = 1, UserName = "testuser" };
		fixture.UserManagerDouble.WithFindByIdAsync("1", selfUser);
		fixture.RoleManagerDouble.WithRoleExistsAsync("User", true);
		fixture.UserManagerDouble.WithAddToRoleAsync(selfUser, "User", IdentityResult.Success);
		fixture.UserManagerDouble.WithUpdateSecurityStampAsync(selfUser, IdentityResult.Success);

		var result = await fixture.Sut.HandleAsync(1, new AssignRoleRequest("User"), CancellationToken.None);

		AssertSuccess(result);
	}
}
