using Microsoft.AspNetCore.Authorization;

namespace JiApp.Common.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
	protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
	{
		if (context.User.IsInRole(RoleNames.Admin) || context.User.HasClaim("permission", requirement.Permission))
			context.Succeed(requirement);

		return Task.CompletedTask;
	}
}
