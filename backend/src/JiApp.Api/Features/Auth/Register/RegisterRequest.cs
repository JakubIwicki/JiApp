namespace JiApp.Api.Features.Auth.Register;

public sealed record RegisterRequest(string Username, string Email, string Password, string DisplayName);
