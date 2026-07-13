using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Admin.Common;
using JiApp.Identity.Features.Admin.Users.EnableUser;
using JiApp.Identity.Tests.Mocks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Identity.Tests.Features.Admin.Users.EnableUser;

public sealed class EnableUserHandlerTests
{
	private sealed class Fixture
	{
		private readonly User _testUser = new()
		{
			Id = 1,
			UserName = "testuser",
			LockoutEnd = DateTimeOffset.MaxValue
		};

		public MockUserManager UserManagerDouble { get; } = MockUserManager.GetSuccessful();
		public MockCurrentUserService CurrentUserMock { get; } = new();

		public AdminAccessGuard Guard { get; }
		public EnableUserHandler Sut { get; }

		public Fixture()
		{
			CurrentUserMock.WithReturning(9999);
			Guard = new AdminAccessGuard(UserManagerDouble.Object, CurrentUserMock.Object);
			Sut = new EnableUserHandler(UserManagerDouble.Object, Guard, Mock.Of<ILogger<EnableUserHandler>>());
		}

		public Fixture WithExistingUser()
		{
			UserManagerDouble.WithFindByIdAsync("1", _testUser);
			UserManagerDouble.WithSetLockoutEndDateAsync(_testUser, null, IdentityResult.Success);
			UserManagerDouble.WithUpdateSecurityStampAsync(_testUser, IdentityResult.Success);
			return this;
		}

		public Fixture WithNonexistentUser()
		{
			UserManagerDouble.WithFindByIdAsync("999", null);
			return this;
		}

		public Fixture WithSelfTarget()
		{
			CurrentUserMock.WithReturning(1);
			return this;
		}
	}

	[Fact]
	public async Task HandleAsync_ReturnsSuccess_WhenEnablingUser()
	{
		var fixture = new Fixture().WithExistingUser();

		var result = await fixture.Sut.HandleAsync(1, CancellationToken.None);

		AssertSuccess(result);
		fixture.UserManagerDouble.VerifySetLockoutEndDateCleared();
		fixture.UserManagerDouble.VerifyUpdatedSecurityStamp_Once();
	}

	[Fact]
	public async Task HandleAsync_ReturnsNotFound_WhenUserDoesNotExist()
	{
		var fixture = new Fixture().WithNonexistentUser();

		var result = await fixture.Sut.HandleAsync(999, CancellationToken.None);

		AssertNotFound(result);
		fixture.UserManagerDouble.VerifyUpdatedSecurityStamp_NotCalled();
	}

	[Fact]
	public async Task HandleAsync_ReturnsAccessDenied_WhenSelfTargeting()
	{
		var fixture = new Fixture().WithSelfTarget();

		var result = await fixture.Sut.HandleAsync(1, CancellationToken.None);

		AssertAccessDenied(result);
		fixture.UserManagerDouble.VerifyUpdatedSecurityStamp_NotCalled();
	}
}
