using System.Security.Claims;
using JiApp.Common.Abstractions;
using JiApp.Identity.Features.Admin.Roles.ListRoles;
using JiApp.Identity.Tests.Mocks;
using Microsoft.AspNetCore.Identity;

namespace JiApp.Identity.Tests.Features.Admin.Roles.ListRoles;

public sealed class ListRolesHandlerTests
{
	private sealed class Fixture
	{
		public MockRoleManager RoleManagerDouble { get; } = MockRoleManager.GetSuccessful();

		public ListRolesHandler Sut { get; }

		public Fixture()
		{
			Sut = new ListRolesHandler(RoleManagerDouble.Object);
		}

		public Fixture WithRoles()
		{
			var roles = new List<IdentityRole<long>>
			{
				new("Admin") { Id = 1 },
				new("User") { Id = 2 }
			}.AsQueryable();

			RoleManagerDouble.WithRolesQueryable(roles);
			RoleManagerDouble.WithGetClaimsAsyncByName("Admin",
				[new Claim("permission", "users.manage"), new Claim("permission", "roles.manage")]);
			RoleManagerDouble.WithGetClaimsAsyncByName("User",
				[new Claim("permission", "scheduler.access")]);
			return this;
		}
	}

	[Fact]
	public async Task HandleAsync_ReturnsAllRolesWithPermissions()
	{
		var fixture = new Fixture().WithRoles();

		var result = await fixture.Sut.HandleAsync(CancellationToken.None);

		AssertSuccess(result);
		result.Value!.Roles.Should().HaveCount(2);
		result.Value.Roles.Should().Contain(r => r.Name == "Admin" && r.Permissions.Count == 2);
		result.Value.Roles.Should().Contain(r => r.Name == "User" && r.Permissions.Count == 1);
	}
}
