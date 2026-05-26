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
        var outputTemplate = Path.Combine(outputPath, $"{Guid.NewGuid():N}.%(ext)s");

        var youtubeDl = new YoutubeDL
        {
            YoutubeDLPath = ytDlpPath,
            FFmpegPath = ffmpegPath,
        };

        var options = new OptionSet
        {
            NoPlaylist = true,
            ExtractAudio = true,
            AudioFormat = AudioConversionFormat.Mp3,
            ExtractorArgs = "youtube:player_client=android_vr",
            Output = outputTemplate
        };
        var result = await youtubeDl.RunWithOptions(videoUrl, options, ct: cancellationToken);

        if (!result.Success)
        {
            options.ExtractorArgs = null;
            options.Output = outputTemplate;
            var fallbackResult = await youtubeDl.RunWithOptions(videoUrl, options, ct: cancellationToken);
            if (fallbackResult.Success)
                result = fallbackResult;
        }

        if (result.Success)
        {
            var resolvedPath = result.Data;
            if (string.IsNullOrEmpty(resolvedPath) || !File.Exists(resolvedPath))
            {
                resolvedPath = Directory.GetFiles(outputPath, "*.mp3")
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .FirstOrDefault();
            }

            return new YoutubeClientResponse(resolvedPath, !string.IsNullOrEmpty(resolvedPath), []);
        }

        return new YoutubeClientResponse(null, false, result.ErrorOutput ?? []);
    }

    public async Task<string> ResolveAudioUrlAsync(string videoId)
    {
        var videoUrl = $"https://www.youtube.com/watch?v={videoId}";

        var youtubeDl = new YoutubeDL
        {
            YoutubeDLPath = ytDlpPath,
            FFmpegPath = ffmpegPath,
        };

        var options = new OptionSet
        {
            NoPlaylist = true,
            ExtractAudio = true,
            AudioFormat = AudioConversionFormat.Mp3,
            GetUrl = true,
        };

        var result = await youtubeDl.RunWithOptions(videoUrl, options);

        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"""Failed to resolve audio URL for video "{videoId}": {string.Join(", ", result.ErrorOutput ?? [])}""");
        }

        var audioUrl = result.Data?.Trim();

        if (string.IsNullOrEmpty(audioUrl) || !audioUrl.StartsWith("https://", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"""Resolved audio URL for video "{videoId}" is invalid: '{audioUrl}'""");
        }

        return audioUrl;
    }

    public void Dispose()
    {
        _youTubeService.Dispose();
    }
}