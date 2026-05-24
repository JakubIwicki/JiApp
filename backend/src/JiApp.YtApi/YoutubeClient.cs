using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Google.Apis.YouTube.v3;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace JiApp.YtApi;

public sealed class YoutubeClient(
    string apiKey,
    string ytDlpPath,
    string ffmpegPath) : IYoutubeClient, IDisposable
{
    private readonly YouTubeService _youTubeService = new(new Google.Apis.Services.BaseClientService.Initializer
    {
        ApiKey = apiKey
    });

    public async Task<IReadOnlyList<YoutubeVideo>> SearchVideosAsync(string query, int maxResults = 10)
    {
        var searchRequest = _youTubeService.Search.List("snippet");
        searchRequest.Q = query;
        searchRequest.MaxResults = maxResults;

        var response = await searchRequest.ExecuteAsync();

        var results = response.Items
            .Where(item => item is { Id.Kind: "youtube#video", Snippet: not null })
            .Select(item => new YoutubeVideo(
                item.Id.VideoId,
                WebUtility.HtmlDecode(item.Snippet.Title),
                WebUtility.HtmlDecode(item.Snippet.Description),
                item.Snippet.Thumbnails.Default__.Url,
                WebUtility.HtmlDecode(item.Snippet.ChannelTitle)))
            .ToList();

        return results.AsReadOnly();
    }

    public async Task<YoutubeClientResponse> DownloadVideoAsync(string videoId, string outputPath,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(outputPath);

        var videoUrl = $"https://www.youtube.com/watch?v={videoId}";
        var youtubeDl = new YoutubeDL
        {
            YoutubeDLPath = ytDlpPath,
            FFmpegPath = ffmpegPath,
            OutputFileTemplate = Path.Combine(outputPath, $"{Guid.NewGuid():N}.%(ext)s")
        };

        var options = new OptionSet
        {
            NoPlaylist = true,
            ExtractAudio = true,
            AudioFormat = AudioConversionFormat.Mp3,
            ExtractorArgs = "youtube:player_client=android_vr"
        };
        var result = await youtubeDl.RunWithOptions(videoUrl, options, ct: cancellationToken);

        if (!result.Success)
        {
            options.ExtractorArgs = null;
            var fallbackResult = await youtubeDl.RunWithOptions(videoUrl, options, ct: cancellationToken);
            if (fallbackResult.Success)
                result = fallbackResult;
        }

        return result.Success
            ? new YoutubeClientResponse(result.Data, true, [])
            : new YoutubeClientResponse(null, false, result.ErrorOutput ?? []);
    }

    public void Dispose()
    {
        _youTubeService.Dispose();
    }
}