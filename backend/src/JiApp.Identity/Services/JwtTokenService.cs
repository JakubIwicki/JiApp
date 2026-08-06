using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JiApp.Common;
using JiApp.Common.Authentication;
using JiApp.Common.Constants;
using Microsoft.IdentityModel.Tokens;

namespace JiApp.Identity.Services;

public interface IJwtTokenService
{
    string GenerateToken(long userId, string username, IEnumerable<string> roles, IEnumerable<string> permissions, string securityStamp);
    bool IsTokenValid(string token);
    string GetUsernameFromToken(string token);
    long GetUserIdFromToken(string token);
}

public sealed class JwtTokenService(
    string key,
    string issuer,
    string audience,
    int expireMinutes,
    TimeProvider timeProvider) : IJwtTokenService
{
    public const string SecurityStampClaimType = "security_stamp";

    private static readonly JwtSecurityTokenHandler Handler = new();

    public string GenerateToken(long userId, string username, IEnumerable<string> roles, IEnumerable<string> permissions, string securityStamp)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var signingKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(now).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64),
            new(SecurityStampClaimType, securityStamp),
        };

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(permissions.Select(p => new Claim(Permissions.PermissionClaimType, p)));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: now.AddMinutes(expireMinutes),
            signingCredentials: credentials
        );

        return Handler.WriteToken(token);
    }

    public bool IsTokenValid(string token)
    {
        try
        {
            Handler.ValidateToken(token, TokenValidationParametersFactory.Create(key, issuer, audience), out _);
            return true;
        }
        catch (SecurityTokenException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public string GetUsernameFromToken(string token)
    {
        var principal = Handler.ValidateToken(token, TokenValidationParametersFactory.Create(key, issuer, audience), out _);
        var claim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
        return claim?.Value ?? throw new SecurityTokenException("Token does not contain a username claim");
    }

    public long GetUserIdFromToken(string token)
    {
        var principal = Handler.ValidateToken(token, TokenValidationParametersFactory.Create(key, issuer, audience), out _);
        var claim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (claim is null || !long.TryParse(claim.Value, out var userId))
            throw new SecurityTokenException("Token does not contain a valid user id claim");
        return userId;
    }
}