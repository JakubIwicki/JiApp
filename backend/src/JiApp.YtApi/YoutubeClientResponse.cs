using System;
using System.Collections.Generic;

namespace JiApp.YtApi;

[Serializable]
public sealed record YoutubeClientResponse(string? FilePath, bool Success, IReadOnlyList<string> Errors);