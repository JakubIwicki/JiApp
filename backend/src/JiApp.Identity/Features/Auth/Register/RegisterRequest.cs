namespace JiApp.Identity.Features.Auth.Register;

[Serializable]
public sealed record RegisterRequest(string Username, string Email, string Password, string DisplayName);
