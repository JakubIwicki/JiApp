namespace JiApp.Api.Features.Auth.Login;

public sealed record LoginResponse(long Id, string? DisplayName, string Token);