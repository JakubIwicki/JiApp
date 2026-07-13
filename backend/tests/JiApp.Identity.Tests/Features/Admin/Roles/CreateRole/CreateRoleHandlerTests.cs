using JiApp.Common.Abstractions;
using JiApp.Identity.Features.Admin.Roles.CreateRole;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Identity.Tests.Features.Admin.Roles.CreateRole;

public sealed class CreateRoleHandlerTests
{
	private sealed class Fixture
	{
		public Mock<RoleManager<IdentityRole<long>>> RoleManagerMock { get; } = new(
			Mock.Of<IRoleStore<IdentityRole<long>>>(),
			Array.Empty<IRoleValidator<IdentityRole<long>>>(),
			Mock.Of<ILookupNormalizer>(),
			Mock.Of<IdentityErrorDescriber>(),
			Mock.Of<ILogger<RoleManager<IdentityRole<long>>>>());

		public CreateRoleHandler Sut { get; }

		public Fixture()
		{
			Sut = new CreateRoleHandler(RoleManagerMock.Object);
		}

		public Fixture WithNewRoleNameAvailable()
		{
			RoleManagerMock.Setup(x => x.RoleExistsAsync("Moderator"))
				.ReturnsAsync(false);
			RoleManagerMock.Setup(x => x.CreateAsync(It.IsAny<IdentityRole<long>>()))
				.ReturnsAsync(IdentityResult.Success);
			RoleManagerMock
				.Setup(x => x.AddClaimAsync(It.IsAny<IdentityRole<long>>(), It.IsAny<System.Security.Claims.Claim>()))
				.ReturnsAsync(IdentityResult.Success);
			return this;
		}

		public Fixture WithRoleAlreadyExisting()
		{
			RoleManagerMock.Setup(x => x.RoleExistsAsync("Existing"))
				.ReturnsAsync(true);
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
