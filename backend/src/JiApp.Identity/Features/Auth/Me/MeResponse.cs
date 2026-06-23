namespace JiApp.Identity.Features.Auth.Me;

[Serializable]
public sealed record MeResponse(
    long Id,
    string? DisplayName,
    string? Username,
    string? Email,
    IReadOnlyList<string> Modules);