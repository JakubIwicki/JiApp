namespace JiApp.Infrastructure.Services;

public interface ITempFileStore
{
    string Add(string filePath);
    string? Get(string id);
    void CleanupExpired();
}
