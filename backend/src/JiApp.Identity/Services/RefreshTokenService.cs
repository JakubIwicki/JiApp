using System.Security.Cryptography;
using System.Text;
using JiApp.Identity.Models;
using JiApp.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace JiApp.Identity.Services;

public interface IRefreshTokenService
{
    Task<RefreshToken> CreateAsync(long userId, CancellationToken ct);
    Task<RefreshToken?> ValidateAsync(string rawToken, CancellationToken ct);
    Task<bool> RevokeAsync(long refreshTokenId, CancellationToken ct);
    Task RevokeAllForUserAsync(long userId, CancellationToken ct);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
}

public sealed class RefreshTokenService(IdentityDbContext dbContext, int refreshTokenExpireDays) : IRefreshTokenService
{
    private static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexStringLower(bytes);
    }

    public async Task<RefreshToken> CreateAsync(long userId, CancellationToken ct)
    {
        var rawBytes = new byte[64];
        RandomNumberGenerator.Fill(rawBytes);
        var rawToken = Convert.ToBase64String(rawBytes);

        var entity = new RefreshToken
        {
            Token = HashToken(rawToken),
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpireDays),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        dbContext.RefreshTokens.Add(entity);
        await dbContext.SaveChangesAsync(ct);

        // Return a new instance with the raw (unhashed) token.
        // Do NOT mutate entity.Token on the tracked entity — that would persist
        // the raw token to the DB on the next SaveChangesAsync, making lookups fail.
        return new RefreshToken
        {
            Id = entity.Id,
            Token = rawToken,
            UserId = entity.UserId,
            ExpiresAt = entity.ExpiresAt,
            CreatedAt = entity.CreatedAt,
            IsRevoked = entity.IsRevoked
        };
    }

    public async Task<RefreshToken?> ValidateAsync(string rawToken, CancellationToken ct)
    {
        var hashed = HashToken(rawToken);

        // Include revoked tokens so the caller can detect token reuse.
        // Callers must check storedToken.IsRevoked for reuse detection.
        return await dbContext.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(rt =>
                rt.Token == hashed &&
                rt.ExpiresAt > DateTime.UtcNow, ct);
    }

    public async Task<bool> RevokeAsync(long refreshTokenId, CancellationToken ct)
    {
        var rowsAffected = await dbContext.RefreshTokens
            .Where(rt => rt.Id == refreshTokenId && !rt.IsRevoked)
            .ExecuteUpdateAsync(setters => setters.SetProperty(rt => rt.IsRevoked, true), ct);

        return rowsAffected > 0;
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        return await dbContext.Database.BeginTransactionAsync(ct);
    }

    public async Task RevokeAllForUserAsync(long userId, CancellationToken ct)
    {
        await dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ExecuteUpdateAsync(setters => setters.SetProperty(rt => rt.IsRevoked, true), ct);
    }
}