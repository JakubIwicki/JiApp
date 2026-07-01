using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Admin.Common;
using JiApp.Identity.Features.Admin.Users.DisableUser;
using JiApp.Identity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Identity.Tests.Features.Admin.Users.DisableUser;

public sealed class DisableUserHandlerTests
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
		public Mock<IRefreshTokenService> RefreshTokenServiceMock { get; } = new();

		public AdminAccessGuard Guard { get; }
		public DisableUserHandler Sut { get; }

		public Fixture()
		{
			CurrentUserMock.Setup(x => x.UserId).Returns(1);
			Guard = new AdminAccessGuard(UserManagerMock.Object, CurrentUserMock.Object);
			Sut = new DisableUserHandler(UserManagerMock.Object, RefreshTokenServiceMock.Object, Guard);
		}

		public Fixture WithTargetUser()
		{
			UserManagerMock.Setup(x => x.FindByIdAsync("2"))
				.ReturnsAsync(_testUser);
			UserManagerMock.Setup(x => x.IsLockedOutAsync(_testUser))
				.ReturnsAsync(false);
			UserManagerMock.Setup(x => x.SetLockoutEnabledAsync(_testUser, true))
				.ReturnsAsync(IdentityResult.Success);
			UserManagerMock.Setup(x => x.SetLockoutEndDateAsync(_testUser, DateTimeOffset.MaxValue))
				.ReturnsAsync(IdentityResult.Success);
			RefreshTokenServiceMock.Setup(x => x.RevokeAllForUserAsync(2))
				.Returns(Task.CompletedTask);
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
	public async Task HandleAsync_ReturnsSuccess_WhenDisablingAnotherUser()
	{
		var fixture = new Fixture().WithTargetUser();

		var result = await fixture.Sut.HandleAsync(2);

		AssertSuccess(result);
		fixture.RefreshTokenServiceMock.Verify(x => x.RevokeAllForUserAsync(2), Times.Once);
	}

	[Fact]
	public async Task HandleAsync_ReturnsAccessDenied_WhenSelfDisable()
	{
		var fixture = new Fixture().WithTargetAsSelf();

		var result = await fixture.Sut.HandleAsync(2);

		AssertAccessDenied(result);
	}

	[Fact]
	public async Task HandleAsync_ReturnsAccessDenied_WhenDisablingLastAdmin()
	{
		var fixture = new Fixture().WithTargetAsLastAdmin();

		var result = await fixture.Sut.HandleAsync(2);

		AssertAccessDenied(result);
	}

	[Fact]
	public async Task HandleAsync_ReturnsNotFound_WhenUserDoesNotExist()
	{
		var fixture = new Fixture();
		fixture.UserManagerMock.Setup(x => x.FindByIdAsync("999"))
			.ReturnsAsync((User?)null);

		var result = await fixture.Sut.HandleAsync(999);

		AssertNotFound(result);
	}
}
