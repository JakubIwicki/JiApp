namespace JiApp.Identity.Features.Auth.Login;

[Serializable]
public sealed record LoginRequest(string Username, string Password);
