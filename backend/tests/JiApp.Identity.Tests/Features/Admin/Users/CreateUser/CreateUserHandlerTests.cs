using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Admin.Users.CreateUser;
using JiApp.Identity.Tests.Mocks;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace JiApp.Identity.Tests.Features.Admin.Users.CreateUser;

public sealed class CreateUserHandlerTests
{
	private sealed class Fixture
	{
		public MockUserManager UserManagerDouble { get; } = MockUserManager.GetSuccessful();
		public MockRoleManager RoleManagerDouble { get; } = MockRoleManager.GetSuccessful();

		public CreateUserHandler Sut { get; }

		public Fixture()
		{
			Sut = new CreateUserHandler(UserManagerDouble.Object, RoleManagerDouble.Object);
		}

		public Fixture WithRoleExists(string roleName)
		{
			RoleManagerDouble.WithRoleExistsAsync(roleName, true);
			return this;
		}

		public Fixture WithSuccessfulCreate()
		{
			UserManagerDouble.WithCreateAsync("Password1", IdentityResult.Success,
				callback: user => user.Id = 7);
			UserManagerDouble.WithAddToRolesAsyncSuccess();
			return this;
		}

		public Fixture WithFailingCreate()
		{
			UserManagerDouble.WithCreateAsync("weak",
				IdentityResult.Failed(
					new IdentityError { Description = "Password must contain at least one uppercase letter." }));
			return this;
		}

		public Fixture WithUniqueConstraintViolation()
		{
			UserManagerDouble.WithCreateThrowsUniqueConstraint();
			return this;
		}

		public Fixture WithFailingRoleAssignment(long userId)
		{
			UserManagerDouble.WithCreateAsync("Password1", IdentityResult.Success,
				callback: user => user.Id = userId);
			UserManagerDouble.WithAddToRolesAsyncFailure(userId,
				new IdentityError { Description = "Role assignment failed" });
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
			"newuser", "new@test.com", "Password1", "New User", ["User"]), CancellationToken.None);

		AssertSuccess(result);
		result.Value!.UserId.Should().Be(7);
	}

	[Fact]
	public async Task HandleAsync_ReturnsValidationFailure_WhenRoleDoesNotExist()
	{
		var fixture = new Fixture();
		fixture.RoleManagerDouble.WithRoleExistsAsync("FakeRole", false);

		var result = await fixture.Sut.HandleAsync(new CreateUserRequest(
			"newuser", "new@test.com", "Password1", "New User", ["FakeRole"]), CancellationToken.None);

		AssertValidationFailure(result);
	}

	[Fact]
	public async Task HandleAsync_ReturnsFailure_WhenCreateFails()
	{
		var fixture = new Fixture().WithFailingCreate();

		var result = await fixture.Sut.HandleAsync(new CreateUserRequest(
			"newuser", "new@test.com", "weak", "New User", []), CancellationToken.None);

		AssertValidationFailure(result);
	}

	[Fact]
	public async Task HandleAsync_ReturnsConflict_OnUniqueConstraintViolation()
	{
		var fixture = new Fixture().WithUniqueConstraintViolation();

		var result = await fixture.Sut.HandleAsync(new CreateUserRequest(
			"existinguser", "existing@test.com", "Password1", "New User", []), CancellationToken.None);

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
			"newuser", "new@test.com", "Password1", "New User", ["Admin"]), CancellationToken.None);

		fixture.UserManagerDouble.VerifyDeletedUser(createdUserId);
		result.IsSuccess.Should().BeFalse();
	}
}
