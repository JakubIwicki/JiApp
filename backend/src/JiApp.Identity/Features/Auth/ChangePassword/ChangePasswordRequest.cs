namespace JiApp.Identity.Features.Auth.ChangePassword;

[Serializable]
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
