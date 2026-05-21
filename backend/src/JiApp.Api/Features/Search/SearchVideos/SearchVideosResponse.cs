namespace JiApp.Api.Features.Search.SearchVideos;

public sealed record SearchVideosResponse(IReadOnlyList<VideoItem> Results);

public sealed record VideoItem(string VideoId, string Title, string Description, string ImageUrl, string VideoUrl);
