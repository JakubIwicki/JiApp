using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Admin.Users.EnableUser;
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

		public EnableUserHandler Sut { get; }

		public Fixture()
		{
			Sut = new EnableUserHandler(UserManagerMock.Object);
		}

		public Fixture WithExistingUser()
		{
			UserManagerMock.Setup(x => x.FindByIdAsync("1"))
				.ReturnsAsync(_testUser);
			UserManagerMock.Setup(x => x.SetLockoutEndDateAsync(_testUser, null))
				.ReturnsAsync(IdentityResult.Success);
			return this;
		}

		public Fixture WithNonexistentUser()
		{
			UserManagerMock.Setup(x => x.FindByIdAsync("999"))
				.ReturnsAsync((User?)null);
			return this;
		}
	}

	[Fact]
	public async Task HandleAsync_ReturnsSuccess_WhenEnablingUser()
	{
		var fixture = new Fixture().WithExistingUser();

		var result = await fixture.Sut.HandleAsync(1);

		AssertSuccess(result);
		fixture.UserManagerMock.Verify(x => x.SetLockoutEndDateAsync(It.IsAny<User>(), null), Times.Once);
	}

	[Fact]
	public async Task HandleAsync_ReturnsNotFound_WhenUserDoesNotExist()
	{
		var fixture = new Fixture().WithNonexistentUser();

		var result = await fixture.Sut.HandleAsync(999);

		AssertNotFound(result);
	}
}
