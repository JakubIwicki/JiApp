using JiApp.Common.Models;
using JiApp.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Identity.Persistence;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : IdentityDbContext<User, IdentityRole<long>, long>(options)
{
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<UserModuleGrant> UserModuleGrants { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }
}