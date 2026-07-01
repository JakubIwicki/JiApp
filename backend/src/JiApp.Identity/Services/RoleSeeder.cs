using JiApp.Common;
using JiApp.Common.Models;
using JiApp.Identity.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace JiApp.Identity.Services;

public interface IRoleSeeder
{
	Task SeedAsync(CancellationToken ct = default);
}

public sealed class RoleSeeder(
	RoleManager<IdentityRole<long>> roleManager,
	UserManager<User> userManager,
	IdentitySettings settings,
	ILogger<RoleSeeder> logger) : IRoleSeeder
{
	private static readonly (string Name, string[] Permissions)[] RoleDefinitions =
	[
		(RoleNames.Admin, Permissions.All),
		(RoleNames.User, Permissions.ModuleAccess),
		(RoleNames.Guest, []),
	];

	public async Task SeedAsync(CancellationToken ct = default)
	{
		foreach (var (name, desiredPermissions) in RoleDefinitions)
		{
			if (name == RoleNames.Admin)
				await SeedAndReconcileRoleAsync(name, desiredPermissions, ct);
			else
				await SeedRoleCreateOnlyAsync(name, desiredPermissions, ct);
		}

		await BootstrapAdminAsync(ct);
	}

	private async Task SeedAndReconcileRoleAsync(string name, string[] desiredPermissions, CancellationToken ct)
	{
		var role = await roleManager.FindByNameAsync(name);
		if (role is null)
		{
			role = new IdentityRole<long>(name);
			var result = await roleManager.CreateAsync(role);
			if (!result.Succeeded)
			{
				logger.LogWarning("Failed to create role {RoleName}: {Errors}", name,
					string.Join(", ", result.Errors.Select(e => e.Description)));
				return;
			}

			logger.LogInformation("Created role {RoleName}", name);
		}

		var existingClaims = await roleManager.GetClaimsAsync(role);
		var existingPermissionValues = existingClaims
			.Where(c => c.Type == "permission")
			.Select(c => c.Value)
			.ToHashSet();

		var desiredSet = desiredPermissions.ToHashSet();

		foreach (var claim in existingClaims)
		{
			if (claim.Type == "permission" && !desiredSet.Contains(claim.Value))
			{
				await roleManager.RemoveClaimAsync(role, claim);
				logger.LogInformation("Removed permission {Permission} from role {RoleName}", claim.Value, name);
			}
		}

		foreach (var permission in desiredPermissions)
		{
			if (!existingPermissionValues.Contains(permission))
			{
				await roleManager.AddClaimAsync(role, new System.Security.Claims.Claim("permission", permission));
				logger.LogInformation("Added permission {Permission} to role {RoleName}", permission, name);
			}
		}
	}

	private async Task SeedRoleCreateOnlyAsync(string name, string[] defaultPermissions, CancellationToken ct)
	{
		var role = await roleManager.FindByNameAsync(name);
		if (role is not null)
		{
			logger.LogDebug("Role {RoleName} already exists; skipping seed to preserve admin edits", name);
			return;
		}

		role = new IdentityRole<long>(name);
		var result = await roleManager.CreateAsync(role);
		if (!result.Succeeded)
		{
			logger.LogWarning("Failed to create role {RoleName}: {Errors}", name,
				string.Join(", ", result.Errors.Select(e => e.Description)));
			return;
		}

		logger.LogInformation("Created role {RoleName}", name);

		foreach (var permission in defaultPermissions)
		{
			await roleManager.AddClaimAsync(role, new System.Security.Claims.Claim("permission", permission));
		}

		if (defaultPermissions.Length > 0)
			logger.LogInformation("Seeded {Count} default permissions for role {RoleName}", defaultPermissions.Length, name);
	}

	private async Task BootstrapAdminAsync(CancellationToken ct)
	{
		var adminUsername = settings.Bootstrap?.AdminUsername;
		if (string.IsNullOrEmpty(adminUsername))
		{
			logger.LogDebug("No bootstrap admin username configured; skipping admin bootstrap");
			return;
		}

		var admins = await userManager.GetUsersInRoleAsync(RoleNames.Admin);
		if (admins.Count > 0)
		{
			logger.LogDebug("Admin role already has members; skipping bootstrap");
			return;
		}

		var user = await userManager.FindByNameAsync(adminUsername);
		if (user is null)
		{
			logger.LogWarning("Bootstrap admin user '{Username}' not found; register an account first", adminUsername);
			return;
		}

		var result = await userManager.AddToRoleAsync(user, RoleNames.Admin);
		if (result.Succeeded)
			logger.LogInformation("Bootstrapped admin user '{Username}'", adminUsername);
		else
			logger.LogWarning("Failed to bootstrap admin user '{Username}': {Errors}", adminUsername,
				string.Join(", ", result.Errors.Select(e => e.Description)));
	}
}
