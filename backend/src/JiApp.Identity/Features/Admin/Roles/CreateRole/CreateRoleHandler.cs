using System.Security.Claims;
using JiApp.Common;
using JiApp.Common.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace JiApp.Identity.Features.Admin.Roles.CreateRole;

public sealed class CreateRoleHandler(RoleManager<IdentityRole<long>> roleManager)
{
	public async Task<Result<bool>> HandleAsync(CreateRoleRequest request, CancellationToken ct)
	{
		if (await roleManager.RoleExistsAsync(request.Name))
			return Result<bool>.Failure($"Role '{request.Name}' already exists", ResultCategories.Conflict);

		var invalidPermissions = request.Permissions
			.Where(p => !Permissions.All.Contains(p))
			.ToArray();

		if (invalidPermissions.Length > 0)
			return Result<bool>.Failure(
				$"Invalid permissions: {string.Join(", ", invalidPermissions)}",
				ResultCategories.Validation);

		var role = new IdentityRole<long>(request.Name);
		var createResult = await roleManager.CreateAsync(role);
		if (!createResult.Succeeded)
		{
			var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
			return Result<bool>.Failure(errors, ResultCategories.Validation);
		}

		foreach (var permission in request.Permissions)
		{
			await roleManager.AddClaimAsync(role, new Claim("permission", permission));
		}

		return Result<bool>.Success(true);
	}
}
