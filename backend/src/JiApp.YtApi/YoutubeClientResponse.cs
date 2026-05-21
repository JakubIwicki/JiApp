namespace JiApp.YtApi;

public sealed record YoutubeClientResponse(string? FilePath, bool Success, string[] Errors);
