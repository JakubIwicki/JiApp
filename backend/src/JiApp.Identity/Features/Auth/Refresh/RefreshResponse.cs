namespace JiApp.Identity.Features.Auth.Refresh;

[Serializable]
public sealed record RefreshResponse(string AccessToken, string RefreshToken, int ExpiresIn);