namespace JiApp.Identity.Features.Auth.Login;

[Serializable]
public sealed record LoginResponse(
    long UserId,
    string? DisplayName,
    string AccessToken,
    string RefreshToken,
    int ExpiresIn);