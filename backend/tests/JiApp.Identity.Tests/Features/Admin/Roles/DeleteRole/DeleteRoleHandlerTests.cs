using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Admin.Common;
using JiApp.Identity.Features.Admin.Roles.DeleteRole;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Identity.Tests.Features.Admin.Roles.DeleteRole;

public sealed class DeleteRoleHandlerTests
{
	private sealed class Fixture
	{
		private readonly IdentityRole<long> _role = new("Moderator") { Id = 3 };

		public Mock<RoleManager<IdentityRole<long>>> RoleManagerMock { get; } = new(
			Mock.Of<IRoleStore<IdentityRole<long>>>(),
			Array.Empty<IRoleValidator<IdentityRole<long>>>(),
			Mock.Of<ILookupNormalizer>(),
			Mock.Of<IdentityErrorDescriber>(),
			Mock.Of<ILogger<RoleManager<IdentityRole<long>>>>());

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
		public DeleteRoleHandler Sut { get; }

		public Fixture()
		{
			CurrentUserMock.Setup(x => x.UserId).Returns(1);
			Guard = new AdminAccessGuard(UserManagerMock.Object, CurrentUserMock.Object);
			Sut = new DeleteRoleHandler(RoleManagerMock.Object, UserManagerMock.Object, Guard);
		}

		public Fixture WithDeletableRole()
		{
			RoleManagerMock.Setup(x => x.FindByNameAsync("Moderator"))
				.ReturnsAsync(_role);
			UserManagerMock.Setup(x => x.GetUsersInRoleAsync("Moderator"))
				.ReturnsAsync([]);
			RoleManagerMock.Setup(x => x.DeleteAsync(_role))
				.ReturnsAsync(IdentityResult.Success);
			return this;
		}

		public Fixture WithRoleHavingUsers()
		{
			RoleManagerMock.Setup(x => x.FindByNameAsync("Moderator"))
				.ReturnsAsync(_role);
			UserManagerMock.Setup(x => x.GetUsersInRoleAsync("Moderator"))
				.ReturnsAsync([new User { Id = 1, UserName = "someuser" }]);
			return this;
		}
	}

	[Fact]
	public async Task HandleAsync_ReturnsSuccess_WhenDeletingCustomRoleWithNoUsers()
	{
		var fixture = new Fixture().WithDeletableRole();

		var result = await fixture.Sut.HandleAsync("Moderator");

		AssertSuccess(result);
	}

	[Fact]
	public async Task HandleAsync_ReturnsAccessDenied_WhenDeletingReservedRole()
	{
		var fixture = new Fixture();

		var result = await fixture.Sut.HandleAsync("Admin");

		AssertAccessDenied(result);
	}

	[Fact]
	public async Task HandleAsync_ReturnsAccessDenied_WhenDeletingGuestRole()
	{
		var fixture = new Fixture();

		var result = await fixture.Sut.HandleAsync("Guest");

		AssertAccessDenied(result);
	}

	[Fact]
	public async Task HandleAsync_ReturnsConflict_WhenRoleHasUsers()
	{
		var fixture = new Fixture().WithRoleHavingUsers();

		var result = await fixture.Sut.HandleAsync("Moderator");

		AssertConflict(result);
	}

	[Fact]
	public async Task HandleAsync_ReturnsNotFound_WhenRoleDoesNotExist()
	{
		var fixture = new Fixture();
		fixture.RoleManagerMock.Setup(x => x.FindByNameAsync("FakeRole"))
			.ReturnsAsync((IdentityRole<long>?)null);

		var result = await fixture.Sut.HandleAsync("FakeRole");

		AssertNotFound(result);
	}
}
