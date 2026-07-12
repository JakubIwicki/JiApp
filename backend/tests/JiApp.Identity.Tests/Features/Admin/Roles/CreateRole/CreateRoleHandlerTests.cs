using JiApp.Common.Abstractions;
using JiApp.Identity.Features.Admin.Roles.CreateRole;
using JiApp.Identity.Tests.Mocks;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace JiApp.Identity.Tests.Features.Admin.Roles.CreateRole;

public sealed class CreateRoleHandlerTests
{
	private sealed class Fixture
	{
		public MockRoleManager RoleManagerDouble { get; } = MockRoleManager.GetSuccessful();

		public CreateRoleHandler Sut { get; }

		public Fixture()
		{
			Sut = new CreateRoleHandler(RoleManagerDouble.Object);
		}

		public Fixture WithNewRoleNameAvailable()
		{
			RoleManagerDouble.WithRoleExistsAsync("Moderator", false);
			RoleManagerDouble.WithCreateAsync(IdentityResult.Success);
			RoleManagerDouble.WithAddClaimAsync(IdentityResult.Success);
			return this;
		}

		public Fixture WithRoleAlreadyExisting()
		{
			RoleManagerDouble.WithRoleExistsAsync("Existing", true);
			return this;
		}
	}

	[Fact]
	public async Task HandleAsync_ReturnsSuccess_ForValidRole()
	{
		var fixture = new Fixture().WithNewRoleNameAvailable();

		var result = await fixture.Sut.HandleAsync(new CreateRoleRequest(
			"Moderator", ["scheduler.access"]), CancellationToken.None);

		AssertSuccess(result);
	}

	[Fact]
	public async Task HandleAsync_ReturnsConflict_WhenRoleAlreadyExists()
	{
		var fixture = new Fixture().WithRoleAlreadyExisting();

		var result = await fixture.Sut.HandleAsync(new CreateRoleRequest(
			"Existing", ["scheduler.access"]), CancellationToken.None);

		AssertConflict(result);
	}

	[Fact]
	public async Task HandleAsync_ReturnsValidationFailure_WhenPermissionsInvalid()
	{
		var fixture = new Fixture().WithNewRoleNameAvailable();

		var result = await fixture.Sut.HandleAsync(new CreateRoleRequest(
			"Moderator", ["invalid.permission"]), CancellationToken.None);

		AssertValidationFailure(result);
	}
}
