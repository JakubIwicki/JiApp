using System.Security.Claims;
using JiApp.Common.Abstractions;
using JiApp.Identity.Features.Admin.Roles.ListRoles;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Identity.Tests.Features.Admin.Roles.ListRoles;

public sealed class ListRolesHandlerTests
{
	private sealed class Fixture
	{
		public Mock<RoleManager<IdentityRole<long>>> RoleManagerMock { get; } = new(
			Mock.Of<IRoleStore<IdentityRole<long>>>(),
			Array.Empty<IRoleValidator<IdentityRole<long>>>(),
			Mock.Of<ILookupNormalizer>(),
			Mock.Of<IdentityErrorDescriber>(),
			Mock.Of<ILogger<RoleManager<IdentityRole<long>>>>());

		public ListRolesHandler Sut { get; }

		public Fixture()
		{
			Sut = new ListRolesHandler(RoleManagerMock.Object);
		}

		public Fixture WithRoles()
		{
			var roles = new List<IdentityRole<long>>
			{
				new("Admin") { Id = 1 },
				new("User") { Id = 2 }
			}.AsQueryable();

			RoleManagerMock.Setup(x => x.Roles).Returns(roles);
			RoleManagerMock
				.Setup(x => x.GetClaimsAsync(It.Is<IdentityRole<long>>(r => r.Name == "Admin")))
				.ReturnsAsync([new Claim("permission", "users.manage"), new Claim("permission", "roles.manage")]);
			RoleManagerMock
				.Setup(x => x.GetClaimsAsync(It.Is<IdentityRole<long>>(r => r.Name == "User")))
				.ReturnsAsync([new Claim("permission", "scheduler.access")]);
			return this;
		}
	}

	[Fact]
	public async Task HandleAsync_ReturnsAllRolesWithPermissions()
	{
		var fixture = new Fixture().WithRoles();

		var result = await fixture.Sut.HandleAsync();

		AssertSuccess(result);
		result.Value!.Roles.Should().HaveCount(2);
		result.Value.Roles.Should().Contain(r => r.Name == "Admin" && r.Permissions.Count == 2);
		result.Value.Roles.Should().Contain(r => r.Name == "User" && r.Permissions.Count == 1);
	}
}
