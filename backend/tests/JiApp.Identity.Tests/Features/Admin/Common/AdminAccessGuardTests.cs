using JiApp.Common;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Admin.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Identity.Tests.Features.Admin.Common;

public sealed class AdminAccessGuardTests
{
	private sealed class Fixture
	{
		private readonly User _adminA = new() { Id = 1, UserName = "adminA" };
		private readonly User _adminB = new() { Id = 2, UserName = "adminB" };

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

		public AdminAccessGuard Sut { get; }

		public Fixture()
		{
			CurrentUserMock.Setup(x => x.UserId).Returns(999);
			Sut = new AdminAccessGuard(UserManagerMock.Object, CurrentUserMock.Object);
		}

		public Fixture WithTwoAdminsOneLockedOut(long targetUserId)
		{
			var target = targetUserId == _adminA.Id ? _adminA : _adminB;
			var other = target == _adminA ? _adminB : _adminA;

			UserManagerMock.Setup(x => x.FindByIdAsync(targetUserId.ToString()))
				.ReturnsAsync(target);
			UserManagerMock.Setup(x => x.IsInRoleAsync(target, RoleNames.Admin))
				.ReturnsAsync(true);
			UserManagerMock.Setup(x => x.GetUsersInRoleAsync(RoleNames.Admin))
				.ReturnsAsync([_adminA, _adminB]);
			UserManagerMock.Setup(x => x.IsLockedOutAsync(target))
				.ReturnsAsync(false);
			UserManagerMock.Setup(x => x.IsLockedOutAsync(other))
				.ReturnsAsync(true);
			return this;
		}

		public Fixture WithTwoAdminsOneLockedOutTargetDisabled(long targetUserId)
		{
			var target = targetUserId == _adminA.Id ? _adminA : _adminB;
			var other = target == _adminA ? _adminB : _adminA;

			UserManagerMock.Setup(x => x.FindByIdAsync(targetUserId.ToString()))
				.ReturnsAsync(target);
			UserManagerMock.Setup(x => x.IsInRoleAsync(target, RoleNames.Admin))
				.ReturnsAsync(true);
			UserManagerMock.Setup(x => x.GetUsersInRoleAsync(RoleNames.Admin))
				.ReturnsAsync([_adminA, _adminB]);
			UserManagerMock.Setup(x => x.IsLockedOutAsync(target))
				.ReturnsAsync(true);
			UserManagerMock.Setup(x => x.IsLockedOutAsync(other))
				.ReturnsAsync(false);
			return this;
		}

		public Fixture WithTwoActiveAdmins(long targetUserId)
		{
			var target = targetUserId == _adminA.Id ? _adminA : _adminB;

			UserManagerMock.Setup(x => x.FindByIdAsync(targetUserId.ToString()))
				.ReturnsAsync(target);
			UserManagerMock.Setup(x => x.IsInRoleAsync(target, RoleNames.Admin))
				.ReturnsAsync(true);
			UserManagerMock.Setup(x => x.GetUsersInRoleAsync(RoleNames.Admin))
				.ReturnsAsync([_adminA, _adminB]);
			UserManagerMock.Setup(x => x.IsLockedOutAsync(_adminA))
				.ReturnsAsync(false);
			UserManagerMock.Setup(x => x.IsLockedOutAsync(_adminB))
				.ReturnsAsync(false);
			return this;
		}
	}

	[Fact]
	public async Task EnsureNotLastAdminAsync_ReturnsAccessDenied_WhenOnlyOneEffectiveAdminRemains()
	{
		var fixture = new Fixture().WithTwoAdminsOneLockedOut(targetUserId: 1);

		var result = await fixture.Sut.EnsureNotLastAdminAsync(1);

		AssertAccessDenied(result);
	}

	[Fact]
	public async Task EnsureNotLastAdminAsync_ReturnsSuccess_WhenTargetIsAlreadyLockedOut()
	{
		var fixture = new Fixture().WithTwoAdminsOneLockedOutTargetDisabled(targetUserId: 2);

		var result = await fixture.Sut.EnsureNotLastAdminAsync(2);

		AssertSuccess(result);
	}

	[Fact]
	public async Task EnsureNotLastAdminAsync_ReturnsSuccess_WhenTwoEffectiveAdminsExist()
	{
		var fixture = new Fixture().WithTwoActiveAdmins(targetUserId: 1);

		var result = await fixture.Sut.EnsureNotLastAdminAsync(1);

		AssertSuccess(result);
	}

	[Fact]
	public async Task EnsureNotLastAdminAsync_ReturnsSuccess_WhenTargetIsNotAdmin()
	{
		var fixture = new Fixture();
		fixture.UserManagerMock.Setup(x => x.FindByIdAsync("5"))
			.ReturnsAsync(new User { Id = 5, UserName = "regular" });
		fixture.UserManagerMock.Setup(x => x.IsInRoleAsync(It.IsAny<User>(), RoleNames.Admin))
			.ReturnsAsync(false);

		var result = await fixture.Sut.EnsureNotLastAdminAsync(5);

		AssertSuccess(result);
	}

	[Fact]
	public void EnsureNotSelf_ReturnsAccessDenied_WhenTargetIsSelf()
	{
		var fixture = new Fixture();
		fixture.CurrentUserMock.Setup(x => x.UserId).Returns(42);

		var result = fixture.Sut.EnsureNotSelf(42);

		AssertAccessDenied(result);
	}

	[Fact]
	public void EnsureNotSelf_ReturnsSuccess_WhenTargetIsOther()
	{
		var fixture = new Fixture();
		fixture.CurrentUserMock.Setup(x => x.UserId).Returns(42);

		var result = fixture.Sut.EnsureNotSelf(99);

		AssertSuccess(result);
	}
}
