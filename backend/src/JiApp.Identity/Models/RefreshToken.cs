// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace JiApp.Identity.Models;

public sealed class RefreshToken
{
    public long Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public long UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRevoked { get; set; }
}