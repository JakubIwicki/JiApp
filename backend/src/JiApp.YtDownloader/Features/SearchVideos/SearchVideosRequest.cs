namespace JiApp.YtDownloader.Features.SearchVideos;

[Serializable]
public sealed record SearchVideosRequest(string Query, int? MaxResults);
