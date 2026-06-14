// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace JiApp.Common.Models;

public sealed class UserModuleGrant
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public DateTime GrantedAt { get; set; }
}
