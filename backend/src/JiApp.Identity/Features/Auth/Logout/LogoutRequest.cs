namespace JiApp.Identity.Features.Auth.Logout;

[Serializable]
public sealed record LogoutRequest(string RefreshToken);
