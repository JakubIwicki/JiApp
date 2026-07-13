using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Admin.Common;
using JiApp.Identity.Features.Admin.Users.DeleteUser;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

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
		public DeleteUserHandler Sut { get; }

		public Fixture()
		{
			CurrentUserMock.Setup(x => x.UserId).Returns(1);
			Guard = new AdminAccessGuard(UserManagerMock.Object, CurrentUserMock.Object);
			Sut = new DeleteUserHandler(UserManagerMock.Object, Guard);
		}

		public Fixture WithTargetUser()
		{
			UserManagerMock.Setup(x => x.FindByIdAsync("2"))
				.ReturnsAsync(_testUser);
			UserManagerMock.Setup(x => x.IsInRoleAsync(_testUser, "Admin"))
				.ReturnsAsync(false);
			UserManagerMock.Setup(x => x.DeleteAsync(_testUser))
				.ReturnsAsync(IdentityResult.Success);
			return this;
		}

		public Fixture WithTargetAsSelf()
		{
			CurrentUserMock.Setup(x => x.UserId).Returns(2);
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
	public async Task HandleAsync_ReturnsSuccess_WhenDeletingAnotherUser()
	{
		var fixture = new Fixture().WithTargetUser();

		var result = await fixture.Sut.HandleAsync(2, CancellationToken.None);

		AssertSuccess(result);
		fixture.UserManagerMock.Verify(x => x.DeleteAsync(It.IsAny<User>()), Times.Once);
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
		fixture.UserManagerMock.Setup(x => x.FindByIdAsync("999"))
			.ReturnsAsync((User?)null);

		var result = await fixture.Sut.HandleAsync(999, CancellationToken.None);

		AssertNotFound(result);
	}
}
