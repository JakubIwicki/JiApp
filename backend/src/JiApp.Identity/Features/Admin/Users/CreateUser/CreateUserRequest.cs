namespace JiApp.Identity.Features.Admin.Users.CreateUser;

[Serializable]
public sealed record CreateUserRequest(string Username, string Email, string Password, string DisplayName, string[] Roles);
