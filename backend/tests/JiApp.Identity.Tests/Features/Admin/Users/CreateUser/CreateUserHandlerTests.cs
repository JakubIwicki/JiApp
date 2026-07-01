using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Admin.Users.CreateUser;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Identity.Tests.Features.Admin.Users.CreateUser;

public sealed class CreateUserHandlerTests
{
	private sealed class Fixture
	{
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

		public CreateUserHandler Sut { get; }

		public Fixture()
		{
			Sut = new CreateUserHandler(UserManagerMock.Object, RoleManagerMock.Object);
		}

		public Fixture WithRoleExists(string roleName)
		{
			RoleManagerMock.Setup(x => x.RoleExistsAsync(roleName))
				.ReturnsAsync(true);
			return this;
		}

		public Fixture WithSuccessfulCreate()
		{
			UserManagerMock
				.Setup(x => x.CreateAsync(It.IsAny<User>(), "Password1"))
				.Callback<User, string>((user, _) => user.Id = 7)
				.ReturnsAsync(IdentityResult.Success);
			UserManagerMock
				.Setup(x => x.AddToRolesAsync(It.IsAny<User>(), It.IsAny<IEnumerable<string>>()))
				.ReturnsAsync(IdentityResult.Success);
			return this;
		}

		public Fixture WithFailingCreate()
		{
			UserManagerMock
				.Setup(x => x.CreateAsync(It.IsAny<User>(), "weak"))
				.ReturnsAsync(IdentityResult.Failed(
					new IdentityError { Description = "Password must contain at least one uppercase letter." }));
			return this;
		}

		public Fixture WithUniqueConstraintViolation()
		{
			var innerEx = new SqliteException("UNIQUE constraint failed", 19);
			UserManagerMock
				.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
				.ThrowsAsync(new DbUpdateException("An error occurred while saving the entity changes", innerEx));
			return this;
		}

		public Fixture WithFailingRoleAssignment(long userId)
		{
			UserManagerMock
				.Setup(x => x.CreateAsync(It.IsAny<User>(), "Password1"))
				.Callback<User, string>((user, _) => user.Id = userId)
				.ReturnsAsync(IdentityResult.Success);
			UserManagerMock
				.Setup(x => x.AddToRolesAsync(It.Is<User>(u => u.Id == userId), It.IsAny<IEnumerable<string>>()))
				.ReturnsAsync(IdentityResult.Failed(
					new IdentityError { Description = "Role assignment failed" }));
			return this;
		}
	}

	[Fact]
	public async Task HandleAsync_ReturnsSuccess_ForValidRequest()
	{
		var fixture = new Fixture()
			.WithRoleExists("User")
			.WithSuccessfulCreate();

		var result = await fixture.Sut.HandleAsync(new CreateUserRequest(
			"newuser", "new@test.com", "Password1", "New User", ["User"]));

		AssertSuccess(result);
		result.Value!.UserId.Should().Be(7);
	}

	[Fact]
	public async Task HandleAsync_ReturnsValidationFailure_WhenRoleDoesNotExist()
	{
		var fixture = new Fixture();
		fixture.RoleManagerMock.Setup(x => x.RoleExistsAsync("FakeRole"))
			.ReturnsAsync(false);

		var result = await fixture.Sut.HandleAsync(new CreateUserRequest(
			"newuser", "new@test.com", "Password1", "New User", ["FakeRole"]));

		AssertValidationFailure(result);
	}

	[Fact]
	public async Task HandleAsync_ReturnsFailure_WhenCreateFails()
	{
		var fixture = new Fixture().WithFailingCreate();

		var result = await fixture.Sut.HandleAsync(new CreateUserRequest(
			"newuser", "new@test.com", "weak", "New User", []));

		AssertValidationFailure(result);
	}

	[Fact]
	public async Task HandleAsync_ReturnsConflict_OnUniqueConstraintViolation()
	{
		var fixture = new Fixture().WithUniqueConstraintViolation();

		var result = await fixture.Sut.HandleAsync(new CreateUserRequest(
			"existinguser", "existing@test.com", "Password1", "New User", []));

		AssertConflict(result);
	}

	[Fact]
	public async Task HandleAsync_CompensatesUserDeletion_WhenRoleAssignmentFails()
	{
		const long createdUserId = 9;
		var fixture = new Fixture()
			.WithRoleExists("Admin")
			.WithFailingRoleAssignment(createdUserId);

		var result = await fixture.Sut.HandleAsync(new CreateUserRequest(
			"newuser", "new@test.com", "Password1", "New User", ["Admin"]));

		fixture.UserManagerMock.Verify(
			x => x.DeleteAsync(It.Is<User>(u => u.Id == createdUserId)),
			Times.Once);
		result.IsSuccess.Should().BeFalse();
	}
}
