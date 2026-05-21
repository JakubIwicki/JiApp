using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace JiApp.Infrastructure.Services;

public class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    private static readonly JwtSecurityTokenHandler Handler = new();

    private readonly string _key = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured");
    private readonly string _issuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured");
    private readonly string _audience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured");
    private readonly int _expireMinutes = int.Parse(configuration["Jwt:ExpireMinutes"] ?? "30");

    private TokenValidationParameters CreateValidationParameters()
    {
        var keyBytes = Encoding.UTF8.GetBytes(_key);
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    }

    public string GenerateToken(long userId, string username)
    {
        var keyBytes = Encoding.UTF8.GetBytes(_key);
        var signingKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expireMinutes),
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
