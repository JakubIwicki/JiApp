namespace JiApp.Infrastructure.Services;

public interface IJwtTokenService
{
    string GenerateToken(long userId, string username);
    bool IsTokenValid(string token);
    string GetUsernameFromToken(string token);
    long GetUserIdFromToken(string token);
}
