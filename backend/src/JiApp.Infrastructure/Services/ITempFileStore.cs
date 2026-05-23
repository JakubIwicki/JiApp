namespace JiApp.Infrastructure.Services;

public interface ITempFileStore
{
    string Add(string filePath, long userId);
    string? Get(string id);
    string? Get(string id, long userId);
    void CleanupExpired();
}
