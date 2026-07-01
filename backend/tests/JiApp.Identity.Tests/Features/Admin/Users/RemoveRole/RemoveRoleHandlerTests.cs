using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Admin.Common;
using JiApp.Identity.Features.Admin.Users.RemoveRole;
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
		public RemoveRoleHandler Sut { get; }

		public Fixture()
		{
			CurrentUserMock.Setup(x => x.UserId).Returns(1);
			Guard = new AdminAccessGuard(UserManagerMock.Object, CurrentUserMock.Object);
			Sut = new RemoveRoleHandler(UserManagerMock.Object, Guard, Mock.Of<ILogger<RemoveRoleHandler>>());
		}

		public Fixture WithTargetUserAndRole(string roleName)
		{
			UserManagerMock.Setup(x => x.FindByIdAsync("2"))
				.ReturnsAsync(_testUser);
			UserManagerMock.Setup(x => x.RemoveFromRoleAsync(_testUser, roleName))
				.ReturnsAsync(IdentityResult.Success);
			UserManagerMock.Setup(x => x.UpdateSecurityStampAsync(_testUser))
				.ReturnsAsync(IdentityResult.Success);
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
	public async Task HandleAsync_ReturnsSuccess_WhenRemovingNonAdminRole()
	{
		var fixture = new Fixture().WithTargetUserAndRole("User");

		var result = await fixture.Sut.HandleAsync(2, "User");

		AssertSuccess(result);
		fixture.UserManagerMock.Verify(x => x.UpdateSecurityStampAsync(It.IsAny<User>()), Times.Once);
	}

	[Fact]
	public async Task HandleAsync_ReturnsAccessDenied_WhenRemovingAdminFromLastAdmin()
	{
		var fixture = new Fixture().WithTargetAsLastAdmin();

		var result = await fixture.Sut.HandleAsync(2, "Admin");

		AssertAccessDenied(result);
		fixture.UserManagerMock.Verify(x => x.UpdateSecurityStampAsync(It.IsAny<User>()), Times.Never);
	}

	[Fact]
	public async Task HandleAsync_ReturnsNotFound_WhenUserDoesNotExist()
	{
		var fixture = new Fixture();
		fixture.UserManagerMock.Setup(x => x.FindByIdAsync("999"))
			.ReturnsAsync((User?)null);

		var result = await fixture.Sut.HandleAsync(999, "User");

		AssertNotFound(result);
		fixture.UserManagerMock.Verify(x => x.UpdateSecurityStampAsync(It.IsAny<User>()), Times.Never);
	}
}
