using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace JiApp.Common.Authentication;

public static class TokenValidationParametersFactory
{
    public static TokenValidationParameters Create(string key, string issuer, string audience)
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
            ClockSkew = TimeSpan.Zero,
            ValidAlgorithms = ["HS256"]
        };
    }
}
