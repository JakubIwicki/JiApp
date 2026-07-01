using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Admin.Common;
using JiApp.Identity.Features.Admin.Users.AssignRole;
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

		public Mock<RoleManager<IdentityRole<long>>> RoleManagerMock { get; } = new(
			Mock.Of<IRoleStore<IdentityRole<long>>>(),
			Array.Empty<IRoleValidator<IdentityRole<long>>>(),
			Mock.Of<ILookupNormalizer>(),
			Mock.Of<IdentityErrorDescriber>(),
			Mock.Of<ILogger<RoleManager<IdentityRole<long>>>>());

		public Mock<ICurrentUserService> CurrentUserMock { get; } = new();

		public AdminAccessGuard Guard { get; }
		public AssignRoleHandler Sut { get; }

		public Fixture()
		{
			CurrentUserMock.Setup(x => x.UserId).Returns(999);
			Guard = new AdminAccessGuard(UserManagerMock.Object, CurrentUserMock.Object);
			Sut = new AssignRoleHandler(UserManagerMock.Object, RoleManagerMock.Object, Guard);
		}

		public Fixture WithExistingUserAndRole()
		{
			UserManagerMock.Setup(x => x.FindByIdAsync("1"))
				.ReturnsAsync(_testUser);
			RoleManagerMock.Setup(x => x.RoleExistsAsync("Admin"))
				.ReturnsAsync(true);
			UserManagerMock.Setup(x => x.AddToRoleAsync(_testUser, "Admin"))
				.ReturnsAsync(IdentityResult.Success);
			return this;
		}

		public Fixture WithNonexistentRole()
		{
			RoleManagerMock.Setup(x => x.RoleExistsAsync("FakeRole"))
				.ReturnsAsync(false);
			return this;
		}

		public Fixture WithSelfTarget()
		{
			CurrentUserMock.Setup(x => x.UserId).Returns(1);
			return this;
		}
	}

	[Fact]
	public async Task HandleAsync_ReturnsSuccess_WhenAssigningValidRole()
	{
		var fixture = new Fixture().WithExistingUserAndRole();

		var result = await fixture.Sut.HandleAsync(1, new AssignRoleRequest("Admin"));

		AssertSuccess(result);
	}

	[Fact]
	public async Task HandleAsync_ReturnsValidationFailure_WhenRoleDoesNotExist()
	{
		var fixture = new Fixture().WithNonexistentRole();

		var result = await fixture.Sut.HandleAsync(1, new AssignRoleRequest("FakeRole"));

		AssertValidationFailure(result);
	}

	[Fact]
	public async Task HandleAsync_ReturnsNotFound_WhenUserDoesNotExist()
	{
		var fixture = new Fixture();
		fixture.UserManagerMock.Setup(x => x.FindByIdAsync("9999"))
			.ReturnsAsync((User?)null);
		fixture.RoleManagerMock.Setup(x => x.RoleExistsAsync("Admin"))
			.ReturnsAsync(true);

		var result = await fixture.Sut.HandleAsync(9999, new AssignRoleRequest("Admin"));

		AssertNotFound(result);
	}

	[Fact]
	public async Task HandleAsync_ReturnsAccessDenied_WhenSelfAssigningAdminRole()
	{
		var fixture = new Fixture().WithSelfTarget();

		var result = await fixture.Sut.HandleAsync(1, new AssignRoleRequest("Admin"));

		AssertAccessDenied(result);
	}

	[Fact]
	public async Task HandleAsync_ReturnsSuccess_WhenSelfAssigningNonAdminRole()
	{
		var fixture = new Fixture().WithSelfTarget();
		fixture.UserManagerMock.Setup(x => x.FindByIdAsync("1"))
			.ReturnsAsync(new User { Id = 1, UserName = "testuser" });
		fixture.RoleManagerMock.Setup(x => x.RoleExistsAsync("User"))
			.ReturnsAsync(true);
		fixture.UserManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), "User"))
			.ReturnsAsync(IdentityResult.Success);

		var result = await fixture.Sut.HandleAsync(1, new AssignRoleRequest("User"));

		AssertSuccess(result);
	}
}
