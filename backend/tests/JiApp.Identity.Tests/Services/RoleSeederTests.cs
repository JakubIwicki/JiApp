using System.Security.Claims;
using JiApp.Common;
using JiApp.Common.Models;
using JiApp.Identity.Configuration;
using JiApp.Identity.Services;
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

		public Mock<RoleManager<IdentityRole<long>>> RoleManagerMock { get; } = new(
			Mock.Of<IRoleStore<IdentityRole<long>>>(),
			Array.Empty<IRoleValidator<IdentityRole<long>>>(),
			Mock.Of<ILookupNormalizer>(),
			Mock.Of<IdentityErrorDescriber>(),
			Mock.Of<ILogger<RoleManager<IdentityRole<long>>>>());

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

		public IdentitySettings Settings { get; } = new()
		{
			Bootstrap = null
		};

		public Mock<ILogger<RoleSeeder>> LoggerMock { get; } = new();

		public RoleSeeder Sut { get; }

		public Fixture()
		{
			Sut = new RoleSeeder(RoleManagerMock.Object, UserManagerMock.Object, Settings, LoggerMock.Object);
		}

		public Fixture WithAllRolesExist()
		{
			RoleManagerMock.Setup(x => x.FindByNameAsync("Admin"))
				.ReturnsAsync(_adminRole);
			RoleManagerMock.Setup(x => x.FindByNameAsync("User"))
				.ReturnsAsync(_userRole);
			RoleManagerMock.Setup(x => x.FindByNameAsync("Guest"))
				.ReturnsAsync(new IdentityRole<long>("Guest") { Id = 3 });

			// Admin has altered claims (missing most, has an extra)
			RoleManagerMock.Setup(x => x.GetClaimsAsync(_adminRole))
				.ReturnsAsync([
					new Claim("permission", "some.other"),
					new Claim("permission", Permissions.SchedulerAccess)
				]);

			// User has custom claims (different from defaults)
			RoleManagerMock.Setup(x => x.GetClaimsAsync(_userRole))
				.ReturnsAsync([
					new Claim("permission", "custom.read"),
					new Claim("permission", "custom.write")
				]);

			// Guest has claims (should be empty by default)
			RoleManagerMock.Setup(x => x.GetClaimsAsync(It.Is<IdentityRole<long>>(r => r.Name == "Guest")))
				.ReturnsAsync([
					new Claim("permission", "something.extra")
				]);

			RoleManagerMock.Setup(x => x.RemoveClaimAsync(It.IsAny<IdentityRole<long>>(), It.IsAny<Claim>()))
				.ReturnsAsync(IdentityResult.Success);
			RoleManagerMock.Setup(x => x.AddClaimAsync(It.IsAny<IdentityRole<long>>(), It.IsAny<Claim>()))
				.ReturnsAsync(IdentityResult.Success);

			return this;
		}
	}

	[Fact]
	public async Task SeedAsync_ReconcilesAdminPermissions_WhenAdminRoleHasBeenAltered()
	{
		var fixture = new Fixture().WithAllRolesExist();

		await fixture.Sut.SeedAsync();

		fixture.RoleManagerMock.Verify(
			x => x.RemoveClaimAsync(It.Is<IdentityRole<long>>(r => r.Name == "Admin"), It.IsAny<Claim>()),
			Times.AtLeastOnce);
		fixture.RoleManagerMock.Verify(
			x => x.AddClaimAsync(It.Is<IdentityRole<long>>(r => r.Name == "Admin"), It.IsAny<Claim>()),
			Times.AtLeastOnce);
	}

	[Fact]
	public async Task SeedAsync_PreservesUserPermissions_WhenUserRoleAlreadyExists()
	{
		var fixture = new Fixture().WithAllRolesExist();

		await fixture.Sut.SeedAsync();

		fixture.RoleManagerMock.Verify(
			x => x.RemoveClaimAsync(It.Is<IdentityRole<long>>(r => r.Name == "User"), It.IsAny<Claim>()),
			Times.Never);
		fixture.RoleManagerMock.Verify(
			x => x.AddClaimAsync(It.Is<IdentityRole<long>>(r => r.Name == "User"), It.IsAny<Claim>()),
			Times.Never);
	}

	[Fact]
	public async Task SeedAsync_PreservesGuestPermissions_WhenGuestRoleAlreadyExists()
	{
		var fixture = new Fixture().WithAllRolesExist();

		await fixture.Sut.SeedAsync();

		fixture.RoleManagerMock.Verify(
			x => x.RemoveClaimAsync(It.Is<IdentityRole<long>>(r => r.Name == "Guest"), It.IsAny<Claim>()),
			Times.Never);
		fixture.RoleManagerMock.Verify(
			x => x.AddClaimAsync(It.Is<IdentityRole<long>>(r => r.Name == "Guest"), It.IsAny<Claim>()),
			Times.Never);
	}

	[Fact]
	public async Task SeedAsync_CreatesMissingRoles_WhenRolesDoNotExist()
	{
		var fixture = new Fixture();
		fixture.RoleManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>()))
			.ReturnsAsync((IdentityRole<long>?)null);
		fixture.RoleManagerMock.Setup(x => x.CreateAsync(It.IsAny<IdentityRole<long>>()))
			.ReturnsAsync(IdentityResult.Success);
		fixture.RoleManagerMock.Setup(x => x.GetClaimsAsync(It.IsAny<IdentityRole<long>>()))
			.ReturnsAsync([]);
		fixture.RoleManagerMock.Setup(x => x.AddClaimAsync(It.IsAny<IdentityRole<long>>(), It.IsAny<Claim>()))
			.ReturnsAsync(IdentityResult.Success);

		await fixture.Sut.SeedAsync();

		fixture.RoleManagerMock.Verify(
			x => x.CreateAsync(It.IsAny<IdentityRole<long>>()),
			Times.Exactly(3));
	}

	[Fact]
	public async Task SeedAsync_DoesNotCallBootstrap_WhenNoAdminUsernameConfigured()
	{
		var fixture = new Fixture().WithAllRolesExist();

		await fixture.Sut.SeedAsync();

		fixture.UserManagerMock.Verify(
			x => x.GetUsersInRoleAsync(RoleNames.Admin),
			Times.Never);
	}
}
