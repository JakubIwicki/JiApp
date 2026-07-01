using System.Security.Claims;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Admin.Common;
using JiApp.Identity.Features.Admin.Roles.UpdateRolePermissions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Identity.Tests.Features.Admin.Roles.UpdateRolePermissions;

public sealed class UpdateRolePermissionsHandlerTests
{
	private sealed class Fixture
	{
		private readonly IdentityRole<long> _role = new("User") { Id = 2 };

		public Mock<RoleManager<IdentityRole<long>>> RoleManagerMock { get; } = new(
			Mock.Of<IRoleStore<IdentityRole<long>>>(),
			Array.Empty<IRoleValidator<IdentityRole<long>>>(),
			Mock.Of<ILookupNormalizer>(),
			Mock.Of<IdentityErrorDescriber>(),
			Mock.Of<ILogger<RoleManager<IdentityRole<long>>>>());

		public Mock<UserManager<User>> GuardUserManagerMock { get; } = new(
			Mock.Of<IUserStore<User>>(),
			Mock.Of<Microsoft.Extensions.Options.IOptions<IdentityOptions>>(),
			Mock.Of<IPasswordHasher<User>>(),
			Array.Empty<IUserValidator<User>>(),
			Array.Empty<IPasswordValidator<User>>(),
			Mock.Of<ILookupNormalizer>(),
			Mock.Of<IdentityErrorDescriber>(),
			Mock.Of<IServiceProvider>(),
			Mock.Of<ILogger<UserManager<User>>>());

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

		public AdminAccessGuard Guard { get; }
		public UpdateRolePermissionsHandler Sut { get; }

		public Fixture()
		{
			CurrentUserMock.Setup(x => x.UserId).Returns(1);
			Guard = new AdminAccessGuard(GuardUserManagerMock.Object, CurrentUserMock.Object);
			Sut = new UpdateRolePermissionsHandler(RoleManagerMock.Object, UserManagerMock.Object, Guard,
				Mock.Of<ILogger<UpdateRolePermissionsHandler>>());
		}

		public Fixture WithEditableRole()
		{
			RoleManagerMock.Setup(x => x.FindByNameAsync("User"))
				.ReturnsAsync(_role);
			RoleManagerMock.Setup(x => x.GetClaimsAsync(_role))
				.ReturnsAsync([new Claim("permission", "scheduler.access")]);
			RoleManagerMock
				.Setup(x => x.RemoveClaimAsync(_role, It.IsAny<Claim>()))
				.ReturnsAsync(IdentityResult.Success);
			RoleManagerMock
				.Setup(x => x.AddClaimAsync(_role, It.IsAny<Claim>()))
				.ReturnsAsync(IdentityResult.Success);
			UserManagerMock.Setup(x => x.GetUsersInRoleAsync("User"))
				.ReturnsAsync([new User { Id = 10, UserName = "user1" }, new User { Id = 20, UserName = "user2" }]);
			UserManagerMock.Setup(x => x.UpdateSecurityStampAsync(It.IsAny<User>()))
				.ReturnsAsync(IdentityResult.Success);
			return this;
		}

		public Fixture WithNonexistentRole()
		{
			RoleManagerMock.Setup(x => x.FindByNameAsync("FakeRole"))
				.ReturnsAsync((IdentityRole<long>?)null);
			return this;
		}
	}

	[Fact]
	public async Task HandleAsync_ReturnsSuccess_WhenUpdatingEditableRole()
	{
		var fixture = new Fixture().WithEditableRole();

		var result = await fixture.Sut.HandleAsync("User", new UpdateRolePermissionsRequest(
			["ytdownloader.access", "scheduler.access"]));

		AssertSuccess(result);
	}

	[Fact]
	public async Task HandleAsync_ReturnsAccessDenied_WhenEditingAdminRole()
	{
		var fixture = new Fixture();

		var result = await fixture.Sut.HandleAsync("Admin", new UpdateRolePermissionsRequest(
			["scheduler.access"]));

		AssertAccessDenied(result);
	}

	[Fact]
	public async Task HandleAsync_ReturnsNotFound_WhenRoleDoesNotExist()
	{
		var fixture = new Fixture().WithNonexistentRole();

		var result = await fixture.Sut.HandleAsync("FakeRole", new UpdateRolePermissionsRequest(
			["scheduler.access"]));

		AssertNotFound(result);
	}

	[Fact]
	public async Task HandleAsync_ReturnsValidationFailure_WhenPermissionsInvalid()
	{
		var fixture = new Fixture().WithEditableRole();

		var result = await fixture.Sut.HandleAsync("User", new UpdateRolePermissionsRequest(
			["invalid.permission"]));

		AssertValidationFailure(result);
	}

	[Fact]
	public async Task HandleAsync_RevokesTokensForAllUsersInRole_WhenPermissionsChange()
	{
		var fixture = new Fixture().WithEditableRole();

		await fixture.Sut.HandleAsync("User", new UpdateRolePermissionsRequest(
			["ytdownloader.access", "scheduler.access"]));

		fixture.UserManagerMock.Verify(x => x.UpdateSecurityStampAsync(It.IsAny<User>()), Times.Exactly(2));
	}

	[Fact]
	public async Task HandleAsync_DoesNotRevokeTokens_WhenPermissionsAreUnchanged()
	{
		var fixture = new Fixture().WithEditableRole();

		await fixture.Sut.HandleAsync("User", new UpdateRolePermissionsRequest(
			["scheduler.access"]));

		fixture.UserManagerMock.Verify(x => x.UpdateSecurityStampAsync(It.IsAny<User>()), Times.Never);
	}

	[Fact]
	public async Task HandleAsync_DoesNotRevokeTokens_WhenRoleIsNotEditable()
	{
		var fixture = new Fixture();

		await fixture.Sut.HandleAsync("Admin", new UpdateRolePermissionsRequest(
			["ytdownloader.access"]));

		fixture.UserManagerMock.Verify(x => x.UpdateSecurityStampAsync(It.IsAny<User>()), Times.Never);
	}

	[Fact]
	public async Task HandleAsync_DoesNotRevokeTokens_WhenRoleNotFound()
	{
		var fixture = new Fixture().WithNonexistentRole();

		await fixture.Sut.HandleAsync("FakeRole", new UpdateRolePermissionsRequest(
			["scheduler.access"]));

		fixture.UserManagerMock.Verify(x => x.UpdateSecurityStampAsync(It.IsAny<User>()), Times.Never);
	}
}
