using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
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
    int expireMinutes) : IJwtTokenService
{
    public const string SecurityStampClaimType = "security_stamp";

    private static readonly JwtSecurityTokenHandler Handler = new();

    public static TokenValidationParameters CreateValidationParameters(string key, string issuer, string audience)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    }

    private TokenValidationParameters CreateValidationParameters()
    {
        return CreateValidationParameters(key, issuer, audience);
    }

    public string GenerateToken(long userId, string username, IEnumerable<string> roles, IEnumerable<string> permissions, string securityStamp)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var signingKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;
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
        claims.AddRange(permissions.Select(p => new Claim("permission", p)));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: credentials
        );

        return Handler.WriteToken(token);
    }

    public bool IsTokenValid(string token)
    {
        try
        {
            Handler.ValidateToken(token, CreateValidationParameters(), out _);
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
        var principal = Handler.ValidateToken(token, CreateValidationParameters(), out _);
        var claim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
        return claim?.Value ?? throw new SecurityTokenException("Token does not contain a username claim");
    }

    public long GetUserIdFromToken(string token)
    {
        var principal = Handler.ValidateToken(token, CreateValidationParameters(), out _);
        var claim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (claim is null || !long.TryParse(claim.Value, out var userId))
            throw new SecurityTokenException("Token does not contain a valid user id claim");
        return userId;
    }
}