using Microsoft.AspNetCore.Authorization;

namespace JiApp.Common.Authorization;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
	public string Permission { get; } = permission;
}
