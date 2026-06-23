namespace JiApp.Identity.Features.Auth.UpdateProfile;

[Serializable]
public sealed record UpdateProfileResponse(long Id, string? DisplayName, string? Username, string? Email);
