using System.Security.Claims;
using JiApp.Common;
using JiApp.Common.Models;
using JiApp.Identity.Configuration;
using JiApp.Identity.Services;
using JiApp.Identity.Tests.Mocks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Identity.Tests.Services;

public sealed class RoleSeederTests
{
	private sealed class Fixture
	{
		private readonly IdentityRole<long> _adminRole = new("Admin") { Id = 1 };
		private readonly IdentityRole<long> _userRole = new("User") { Id = 2 };

		public MockRoleManager RoleManagerDouble { get; } = MockRoleManager.GetSuccessful();
		public MockUserManager UserManagerDouble { get; } = MockUserManager.GetSuccessful();

		public IdentitySettings Settings { get; } = new()
		{
			Bootstrap = null
		};

		public Mock<ILogger<RoleSeeder>> LoggerMock { get; } = new();

		public RoleSeeder Sut { get; }

		public Fixture()
		{
			Sut = new RoleSeeder(RoleManagerDouble.Object, UserManagerDouble.Object, Settings, LoggerMock.Object);
		}

		public Fixture WithAllRolesExist()
		{
			RoleManagerDouble.WithFindByNameAsync("Admin", _adminRole);
			RoleManagerDouble.WithFindByNameAsync("User", _userRole);
			RoleManagerDouble.WithFindByNameAsync("Guest", new IdentityRole<long>("Guest") { Id = 3 });

			// Admin has altered claims (missing most, has an extra)
			RoleManagerDouble.WithGetClaimsAsync(_adminRole, [
				new Claim("permission", "some.other"),
				new Claim("permission", Permissions.SchedulerAccess)
			]);

			// User has custom claims (different from defaults)
			RoleManagerDouble.WithGetClaimsAsync(_userRole, [
				new Claim("permission", "custom.read"),
				new Claim("permission", "custom.write")
			]);

			// Guest has claims (should be empty by default)
			RoleManagerDouble.WithGetClaimsAsyncByName("Guest",
				[new Claim("permission", "something.extra")]);

			RoleManagerDouble.WithRemoveClaimAsync(IdentityResult.Success);
			RoleManagerDouble.WithAddClaimAsync(IdentityResult.Success);

			return this;
		}

		public Fixture WithAllRolesExistWithZeroClaims()
		{
			RoleManagerDouble.WithFindByNameAsync("Admin", _adminRole);
			RoleManagerDouble.WithFindByNameAsync("User", _userRole);
			RoleManagerDouble.WithFindByNameAsync("Guest", new IdentityRole<long>("Guest") { Id = 3 });

			RoleManagerDouble.WithGetClaimsAsyncForAny([]);

			RoleManagerDouble.WithRemoveClaimAsync(IdentityResult.Success);
			RoleManagerDouble.WithAddClaimAsync(IdentityResult.Success);

			return this;
		}

		public Fixture WithUserRoleHavingSinglePermissionClaim()
		{
			RoleManagerDouble.WithFindByNameAsync("Admin", _adminRole);
			RoleManagerDouble.WithFindByNameAsync("User", _userRole);
			RoleManagerDouble.WithFindByNameAsync("Guest", new IdentityRole<long>("Guest") { Id = 3 });

			RoleManagerDouble.WithGetClaimsAsyncForAny([]);
			RoleManagerDouble.WithGetClaimsAsync(_userRole, [new Claim("permission", "custom.read")]);

			RoleManagerDouble.WithRemoveClaimAsync(IdentityResult.Success);
			RoleManagerDouble.WithAddClaimAsync(IdentityResult.Success);

			return this;
		}
	}

	[Fact]
	public async Task SeedAsync_ReconcilesAdminPermissions_WhenAdminRoleHasBeenAltered()
	{
		var fixture = new Fixture().WithAllRolesExist();

		await fixture.Sut.SeedAsync();

		fixture.RoleManagerDouble.VerifyRemovedClaimFromRole("Admin");
		fixture.RoleManagerDouble.VerifyAddedClaimToRole("Admin");
	}

	[Fact]
	public async Task SeedAsync_PreservesUserPermissions_WhenUserRoleAlreadyExists()
	{
		var fixture = new Fixture().WithAllRolesExist();

		await fixture.Sut.SeedAsync();

		fixture.RoleManagerDouble.VerifyRemovedClaimFromRole_NotCalled("User");
		fixture.RoleManagerDouble.VerifyAddedClaimToRole_NotCalled("User");
	}

	[Fact]
	public async Task SeedAsync_PreservesGuestPermissions_WhenGuestRoleAlreadyExists()
	{
		var fixture = new Fixture().WithAllRolesExist();

		await fixture.Sut.SeedAsync();

		fixture.RoleManagerDouble.VerifyRemovedClaimFromRole_NotCalled("Guest");
		fixture.RoleManagerDouble.VerifyAddedClaimToRole_NotCalled("Guest");
	}

	[Fact]
	public async Task SeedAsync_CreatesMissingRoles_WhenRolesDoNotExist()
	{
		var fixture = new Fixture();
		fixture.RoleManagerDouble.WithFindByNameAsyncForAny(null);
		fixture.RoleManagerDouble.WithCreateAsync(IdentityResult.Success);
		fixture.RoleManagerDouble.WithGetClaimsAsyncForAny([]);
		fixture.RoleManagerDouble.WithAddClaimAsync(IdentityResult.Success);

		await fixture.Sut.SeedAsync();

		fixture.RoleManagerDouble.VerifyCreatedRole(3);
	}

	[Fact]
	public async Task SeedAsync_SeedsDefaultUserPermissions_WhenUserRoleExistsWithZeroPermissionClaims()
	{
		var fixture = new Fixture().WithAllRolesExistWithZeroClaims();

		await fixture.Sut.SeedAsync();

		foreach (var permission in Permissions.ModuleAccess)
			fixture.RoleManagerDouble.VerifyAddedPermissionToRole("User", permission);
		fixture.RoleManagerDouble.VerifyAddedClaimsToRole("User", Permissions.ModuleAccess.Length);
		fixture.RoleManagerDouble.VerifyRemovedClaimFromRole_NotCalled("User");
	}

	[Fact]
	public async Task SeedAsync_PreservesUserPermissions_WhenUserRoleHasSinglePermissionClaim()
	{
		var fixture = new Fixture().WithUserRoleHavingSinglePermissionClaim();

		await fixture.Sut.SeedAsync();

		fixture.RoleManagerDouble.VerifyAddedClaimToRole_NotCalled("User");
		fixture.RoleManagerDouble.VerifyRemovedClaimFromRole_NotCalled("User");
	}

	[Fact]
	public async Task SeedAsync_DoesNotSeedGuestPermissions_WhenGuestRoleExistsWithZeroClaims()
	{
		var fixture = new Fixture().WithAllRolesExistWithZeroClaims();

		await fixture.Sut.SeedAsync();

		fixture.RoleManagerDouble.VerifyAddedClaimToRole_NotCalled("Guest");
		fixture.RoleManagerDouble.VerifyRemovedClaimFromRole_NotCalled("Guest");
	}

	[Fact]
	public async Task SeedAsync_DoesNotCallBootstrap_WhenNoAdminUsernameConfigured()
	{
		var fixture = new Fixture().WithAllRolesExist();

		await fixture.Sut.SeedAsync();

		fixture.UserManagerDouble.VerifyGetUsersInRoleAsync_NotCalled();
	}
}
