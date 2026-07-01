using Microsoft.AspNetCore.Authorization;

namespace JiApp.Common.Authorization;

public static class AuthorizationPolicyBuilderExtensions
{
	public static AuthorizationPolicyBuilder RequirePermission(this AuthorizationPolicyBuilder builder, string permission) =>
		builder.AddRequirements(new PermissionRequirement(permission));
}
