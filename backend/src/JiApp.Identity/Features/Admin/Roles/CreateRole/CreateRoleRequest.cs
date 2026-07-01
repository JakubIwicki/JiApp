namespace JiApp.Identity.Features.Admin.Roles.CreateRole;

[Serializable]
public sealed record CreateRoleRequest(string Name, string[] Permissions);
