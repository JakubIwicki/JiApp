namespace JiApp.Identity.Features.Auth.UpdateProfile;

[Serializable]
public sealed record UpdateProfileRequest(string DisplayName, string Email);
