namespace JiApp.Identity.Features.Admin.Roles.ListRoles;

[Serializable]
public sealed record RoleSummary(string Name, IReadOnlyList<string> Permissions);

[Serializable]
public sealed record ListRolesResponse(IReadOnlyList<RoleSummary> Roles);
