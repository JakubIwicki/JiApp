using System.ComponentModel;
using JiApp.Common.Abstractions;
using JiApp.YtDownloader.Agent;
using JiApp.YtDownloader.Features.Assistant;
using JiApp.YtDownloader.Features.DownloadHistory;
using JiApp.YtDownloader.Features.SearchHistory;
using JiApp.YtDownloader.Features.SearchVideos;
using ModelContextProtocol.Server;

namespace JiApp.YtDownloader.Mcp;

[McpServerToolType]
public sealed class YtMcpTools
{
    [McpServerTool(Name = AssistantToolNames.SearchYoutube)]
    [Description("Search YouTube for videos/music by free-text query OR a YouTube URL. Returns matching videos with id, title, description, thumbnail, URL, and channel name.")]
    public static async Task<IReadOnlyList<VideoItem>> SearchYoutube(
        YtAgentToolService toolService,
        ICurrentUserService currentUser,
        [Description("Free-text search query or a YouTube URL (watch, shorts, embed, or youtu.be).")]
        string query,
        [Description("Maximum number of results to return (default: 10).")]
        int? maxResults = null,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        var result = await toolService.SearchAsync(userId, query, maxResults, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
            throw new InvalidOperationException(result.Error ?? "Search failed.");

        return result.Value.Results;
    }

    [McpServerTool(Name = AssistantToolNames.ListSearchHistory)]
    [Description("List recent YouTube search history for the authenticated user. Each entry includes the search text and timestamp.")]
    public static async Task<IReadOnlyList<SearchHistoryItem>> ListSearchHistory(
        YtAgentToolService toolService,
        ICurrentUserService currentUser,
        [Description("Maximum number of history entries to return (default: 10).")]
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        var result = await toolService.ListSearchHistoryAsync(userId, limit, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
            throw new InvalidOperationException(result.Error ?? "Failed to list search history.");

        return result.Value.Items;
    }

    [McpServerTool(Name = AssistantToolNames.ListDownloadHistory)]
    [Description("List recent YouTube download history for the authenticated user. Each entry includes the video title, id, URL, and download timestamp.")]
    public static async Task<IReadOnlyList<DownloadHistoryItem>> ListDownloadHistory(
        YtAgentToolService toolService,
        ICurrentUserService currentUser,
        [Description("Maximum number of history entries to return (default: 10).")]
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        var result = await toolService.ListDownloadHistoryAsync(userId, limit, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
            throw new InvalidOperationException(result.Error ?? "Failed to list download history.");

        return result.Value.Items;
    }

    [McpServerTool(Name = AssistantToolNames.OfferDownload)]
    [Description("Propose a download for a YouTube video. This ONLY creates a download offer — it does NOT download anything and does not consume download quota. The offer is a prerequisite step: the user must explicitly accept the offer before any download is executed.")]
    public static DownloadOffer OfferDownload(
        YtAgentToolService toolService,
        ICurrentUserService currentUser,
        [Description("The YouTube video ID (11-character identifier, e.g. dQw4w9WgXcQ).")]
        string videoId,
        [Description("The full YouTube video URL (e.g. https://www.youtube.com/watch?v=dQw4w9WgXcQ).")]
        string videoUrl,
        [Description("The video title (optional, for display purposes).")]
        string? title = null,
        [Description("The video thumbnail image URL (optional, for display purposes).")]
        string? imageUrl = null)
    {
        _ = currentUser.UserId; // validate JWT claim
        return toolService.BuildDownloadOffer(videoId, videoUrl, title, imageUrl);
    }
}
