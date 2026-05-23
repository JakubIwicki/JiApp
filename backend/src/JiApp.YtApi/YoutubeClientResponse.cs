using System.Collections.Generic;

namespace JiApp.YtApi;

public sealed record YoutubeClientResponse(string? FilePath, bool Success, IReadOnlyList<string> Errors);