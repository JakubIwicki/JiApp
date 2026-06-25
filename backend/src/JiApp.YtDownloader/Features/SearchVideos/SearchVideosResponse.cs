using JiApp.YtApi;

namespace JiApp.YtDownloader.Features.SearchVideos;

[Serializable]
public sealed record SearchVideosResponse(IReadOnlyList<VideoItem> Results, bool HasMore);

[Serializable]
public sealed record VideoItem(
    string VideoId,
    string Title,
    string Description,
    string ImageUrl,
    string VideoUrl,
    string ChannelTitle);