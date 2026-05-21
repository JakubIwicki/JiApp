namespace JiApp.Api.Features.Search.SearchVideos;

public sealed record SearchVideosRequest(string Query, int? MaxResults);
