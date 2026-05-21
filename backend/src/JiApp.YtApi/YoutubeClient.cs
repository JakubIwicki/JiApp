using Google.Apis.YouTube.v3;
using JiApp.YtApi.Configuration;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace JiApp.YtApi;

public sealed class YoutubeClient(YoutubeSettings settings) : IYoutubeClient, IDisposable
{
    private readonly YouTubeService _youTubeService = new(new Google.Apis.Services.BaseClientService.Initializer
    {
        ApiKey = settings.ApiKey
    });

    public async Task<IReadOnlyList<YoutubeVideo>> SearchVideosAsync(string query, int maxResults = 10)
    {
        var searchRequest = _youTubeService.Search.List("snippet");
        searchRequest.Q = query;
        searchRequest.MaxResults = maxResults;

        var response = await searchRequest.ExecuteAsync();

        var results = response.Items
            .Where(item => item.Id.Kind == "youtube#video")
            .Select(item => new YoutubeVideo(
                item.Id.VideoId,
                item.Snippet.Title,
                item.Snippet.Description,
                item.Snippet.Thumbnails.Default__.Url))
            .ToList();

        return results.AsReadOnly();
    }

    public async Task<YoutubeClientResponse> DownloadVideoAsync(string videoUrl, string outputPath)
    {
        Directory.CreateDirectory(outputPath);

        var youtubeDl = new YoutubeDL
        {
            YoutubeDLPath = settings.YtDlpPath,
            FFmpegPath = settings.FfmpegPath,
            OutputFileTemplate = Path.Combine(outputPath, "%(title)s.%(ext)s")
        };

        var result = await youtubeDl.RunAudioDownload(videoUrl, AudioConversionFormat.Mp3);

        return result.Success 
            ? new YoutubeClientResponse(result.Data, true, [])
            : new YoutubeClientResponse(null, false, result.ErrorOutput ?? []);
    }

    public void Dispose()
    {
        _youTubeService.Dispose();
    }
}
