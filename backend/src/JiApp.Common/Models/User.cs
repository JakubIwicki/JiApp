// ReSharper disable PropertyCanBeMadeInitOnly.Global
using Microsoft.AspNetCore.Identity;

namespace JiApp.Common.Models;

public class User : IdentityUser<long>
{
    public string? DisplayName { get; set; }
}
