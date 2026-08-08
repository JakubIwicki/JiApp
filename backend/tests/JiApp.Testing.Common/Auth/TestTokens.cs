using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using JiApp.Common.Constants;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;

namespace JiApp.Testing.Common.Auth;

/// <summary>
/// Single source of truth for minting test JWTs, locked to the module hosts' dev
/// JWT settings (the Key/Issuer/Audience every module's appsettings.Test.json
/// configures). A real HS256 token validated by the host's JwtBearer handler is
/// what drives a full-pipeline request through authentication and authorization.
/// </summary>
public static class TestTokens
{
    public const string JwtKey = "test-key-that-is-at-least-32-chars!";
    public const string JwtIssuer = "JiApp-Identity";
    public const string JwtAudience = "jiapp-gateway";

    /// <summary>
    /// Mints a real HS256 JWT carrying the given identity claims.
    /// </summary>
    /// <param name="userId">The subject, minted as the NameIdentifier claim.</param>
    /// <param name="permissions">One <see cref="Permissions.PermissionClaimType"/> claim per entry.</param>
    /// <param name="roles">One <see cref="ClaimTypes.Role"/> claim per entry.</param>
    /// <param name="securityStamp">An optional security-stamp claim for hosts that recheck it.</param>
    /// <param name="lifetime">Token lifetime; defaults to 15 minutes.</param>
    public static string Mint(
        long userId,
        IEnumerable<string>? permissions = null,
        IEnumerable<string>? roles = null,
        string? securityStamp = null,
        TimeSpan? lifetime = null)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };

        if (permissions is not null)
            claims.AddRange(permissions.Select(p => new Claim(Permissions.PermissionClaimType, p)));
        if (roles is not null)
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        // Matches JiApp.Identity.Services.JwtTokenService.SecurityStampClaimType.
        if (securityStamp is not null)
            claims.Add(new Claim("security_stamp", securityStamp));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromMinutes(15)),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Creates an HttpClient against <paramref name="factory"/> pre-authorized as
    /// <paramref name="userId"/> with the given permission claims.
    /// </summary>
    public static HttpClient CreateAuthenticatedClient<TEntryPoint>(
        WebApplicationFactory<TEntryPoint> factory, long userId, params string[] permissions)
        where TEntryPoint : class
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Mint(userId, permissions));
        return client;
    }
}
