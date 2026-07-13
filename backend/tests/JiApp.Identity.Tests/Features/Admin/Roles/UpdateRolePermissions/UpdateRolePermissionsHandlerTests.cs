using System.Security.Claims;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Admin.Common;
using JiApp.Identity.Features.Admin.Roles.UpdateRolePermissions;
using JiApp.Identity.Tests.Mocks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Identity.Tests.Features.Admin.Roles.UpdateRolePermissions;

public sealed class UpdateRolePermissionsHandlerTests
{
	private sealed class Fixture
	{
		private readonly IdentityRole<long> _role = new("User") { Id = 2 };

		public MockRoleManager RoleManagerDouble { get; } = MockRoleManager.GetSuccessful();
		public MockUserManager GuardUserManagerDouble { get; } = MockUserManager.GetSuccessful();
		public MockUserManager UserManagerDouble { get; } = MockUserManager.GetSuccessful();
		public MockCurrentUserService CurrentUserMock { get; } = new();

		public AdminAccessGuard Guard { get; }
		public UpdateRolePermissionsHandler Sut { get; }

		public Fixture()
		{
			CurrentUserMock.WithReturning(1);
			Guard = new AdminAccessGuard(GuardUserManagerDouble.Object, CurrentUserMock.Object);
			Sut = new UpdateRolePermissionsHandler(RoleManagerDouble.Object, UserManagerDouble.Object, Guard,
				Mock.Of<ILogger<UpdateRolePermissionsHandler>>());
		}

		public Fixture WithEditableRole()
		{
			RoleManagerDouble.WithFindByNameAsync("User", _role);
			RoleManagerDouble.WithGetClaimsAsync(_role,
				[new Claim("permission", "scheduler.access")]);
			RoleManagerDouble.WithRemoveClaimAsync(IdentityResult.Success);
			RoleManagerDouble.WithAddClaimAsync(IdentityResult.Success);
			UserManagerDouble.WithGetUsersInRoleAsync("User",
				[new User { Id = 10, UserName = "user1" }, new User { Id = 20, UserName = "user2" }]);
			UserManagerDouble.WithUpdateSecurityStampAsyncForAny(IdentityResult.Success);
			return this;
		}

		public Fixture WithNonexistentRole()
		{
			RoleManagerDouble.WithFindByNameAsync("FakeRole", null);
			return this;
		}
	}

	[Fact]
	public async Task HandleAsync_ReturnsSuccess_WhenUpdatingEditableRole()
	{
		var fixture = new Fixture().WithEditableRole();

		var result = await fixture.Sut.HandleAsync("User", new UpdateRolePermissionsRequest(
			["ytdownloader.access", "scheduler.access"]), CancellationToken.None);

		AssertSuccess(result);
	}

	[Fact]
	public async Task HandleAsync_ReturnsAccessDenied_WhenEditingAdminRole()
	{
		var fixture = new Fixture();

		var result = await fixture.Sut.HandleAsync("Admin", new UpdateRolePermissionsRequest(
			["scheduler.access"]), CancellationToken.None);

		AssertAccessDenied(result);
	}

	[Fact]
	public async Task HandleAsync_ReturnsNotFound_WhenRoleDoesNotExist()
	{
		var fixture = new Fixture().WithNonexistentRole();

		var result = await fixture.Sut.HandleAsync("FakeRole", new UpdateRolePermissionsRequest(
			["scheduler.access"]), CancellationToken.None);

		AssertNotFound(result);
	}

	[Fact]
	public async Task HandleAsync_ReturnsValidationFailure_WhenPermissionsInvalid()
	{
		var fixture = new Fixture().WithEditableRole();

		var result = await fixture.Sut.HandleAsync("User", new UpdateRolePermissionsRequest(
			["invalid.permission"]), CancellationToken.None);

		AssertValidationFailure(result);
	}

	[Fact]
	public async Task HandleAsync_RevokesTokensForAllUsersInRole_WhenPermissionsChange()
	{
		var fixture = new Fixture().WithEditableRole();

		await fixture.Sut.HandleAsync("User", new UpdateRolePermissionsRequest(
			["ytdownloader.access", "scheduler.access"]), CancellationToken.None);

		fixture.UserManagerDouble.VerifyUpdatedSecurityStamp_Exactly(2);
	}

	[Fact]
	public async Task HandleAsync_DoesNotRevokeTokens_WhenPermissionsAreUnchanged()
	{
		var fixture = new Fixture().WithEditableRole();

		await fixture.Sut.HandleAsync("User", new UpdateRolePermissionsRequest(
			["scheduler.access"]), CancellationToken.None);

		fixture.UserManagerDouble.VerifyUpdatedSecurityStamp_NotCalled();
	}

	[Fact]
	public async Task HandleAsync_DoesNotRevokeTokens_WhenRoleIsNotEditable()
	{
		var fixture = new Fixture();

		await fixture.Sut.HandleAsync("Admin", new UpdateRolePermissionsRequest(
			["ytdownloader.access"]), CancellationToken.None);

		fixture.UserManagerDouble.VerifyUpdatedSecurityStamp_NotCalled();
	}

	[Fact]
	public async Task HandleAsync_DoesNotRevokeTokens_WhenRoleNotFound()
	{
		var fixture = new Fixture().WithNonexistentRole();

		await fixture.Sut.HandleAsync("FakeRole", new UpdateRolePermissionsRequest(
			["scheduler.access"]), CancellationToken.None);

		fixture.UserManagerDouble.VerifyUpdatedSecurityStamp_NotCalled();
	}
}
