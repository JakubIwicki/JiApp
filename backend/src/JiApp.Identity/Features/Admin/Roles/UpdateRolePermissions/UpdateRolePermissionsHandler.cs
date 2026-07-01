using System.Security.Claims;
using JiApp.Common;
using JiApp.Common.Abstractions;
using JiApp.Identity.Features.Admin.Common;
using Microsoft.AspNetCore.Identity;

namespace JiApp.Identity.Features.Admin.Roles.UpdateRolePermissions;

public sealed class UpdateRolePermissionsHandler(
	RoleManager<IdentityRole<long>> roleManager,
	AdminAccessGuard guard)
{
	public async Task<Result<bool>> HandleAsync(string roleName, UpdateRolePermissionsRequest request)
	{
		var editableCheck = guard.EnsureRoleIsEditable(roleName);
		if (!editableCheck.IsSuccess)
			return editableCheck;

		var role = await roleManager.FindByNameAsync(roleName);
		if (role is null)
			return Result<bool>.Failure($"Role '{roleName}' not found", ResultCategories.NotFound);

		var invalidPermissions = request.Permissions
			.Where(p => !Permissions.All.Contains(p))
			.ToArray();

		if (invalidPermissions.Length > 0)
			return Result<bool>.Failure(
				$"Invalid permissions: {string.Join(", ", invalidPermissions)}",
				ResultCategories.Validation);

		var existingClaims = await roleManager.GetClaimsAsync(role);
		var desiredSet = request.Permissions.ToHashSet();

		foreach (var claim in existingClaims)
		{
			if (claim.Type == "permission" && !desiredSet.Contains(claim.Value))
				await roleManager.RemoveClaimAsync(role, claim);
		}

		var existingValues = existingClaims
			.Where(c => c.Type == "permission")
			.Select(c => c.Value)
			.ToHashSet();

		foreach (var permission in request.Permissions)
		{
			if (!existingValues.Contains(permission))
				await roleManager.AddClaimAsync(role, new Claim("permission", permission));
		}

		return Result<bool>.Success(true);
	}
}
