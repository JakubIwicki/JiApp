using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.YouTube.v3;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace JiApp.YtApi;

public interface IYoutubeClient
{
    Task<IReadOnlyList<YoutubeVideo>> SearchVideosAsync(string query, int maxResults = 10,
        CancellationToken cancellationToken = default);

    Task<YoutubeVideo?> GetVideoByIdAsync(string videoId,
        CancellationToken cancellationToken = default);

    Task<YoutubeClientResponse> DownloadVideoAsync(string videoId, string outputPath,
        CancellationToken cancellationToken = default);

    Process BuildPreviewAudioProcess(string videoId);
}

public sealed class YoutubeClient(
    string apiKey,
    string ytDlpPath,
    string ffmpegPath,
    string? cookiesFile = null,
    string? cookiesFromBrowser = null,
    string? proxy = null) : IYoutubeClient, IDisposable
{
    private readonly YouTubeService _youTubeService = new(new Google.Apis.Services.BaseClientService.Initializer
    {
        ApiKey = apiKey
    });

    private readonly YoutubeDL _youtubeDl = new()
    {
        YoutubeDLPath = ytDlpPath,
        FFmpegPath = ffmpegPath,
    };

    private readonly SemaphoreSlim _youtubeDlLock = new(1, 1);

    public async Task<IReadOnlyList<YoutubeVideo>> SearchVideosAsync(string query, int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        var searchRequest = _youTubeService.Search.List("snippet");
        searchRequest.Q = query;
        searchRequest.MaxResults = maxResults;

        var response = await searchRequest.ExecuteAsync(cancellationToken);

        return response.Items
            .Where(item => item is { Id.Kind: "youtube#video", Snippet: not null })
            .Select(MapToYoutubeVideo)
            .ToList()
            .AsReadOnly();
    }

    public async Task<YoutubeVideo?> GetVideoByIdAsync(string videoId,
        CancellationToken cancellationToken = default)
    {
        var listRequest = _youTubeService.Videos.List("snippet");
        listRequest.Id = videoId;
        listRequest.MaxResults = 1;

        var response = await listRequest.ExecuteAsync(cancellationToken);

        return response.Items
            .Where(item => item.Snippet is not null)
            .Select(MapToYoutubeVideo)
            .FirstOrDefault();
    }

    private static YoutubeVideo MapToYoutubeVideo(Google.Apis.YouTube.v3.Data.Video video) =>
        new(
            VideoId: video.Id ?? string.Empty,
            Title: WebUtility.HtmlDecode(video.Snippet?.Title ?? string.Empty),
            Description: WebUtility.HtmlDecode(video.Snippet?.Description ?? string.Empty),
            ImageUrl: video.Snippet?.Thumbnails?.Default__?.Url ?? string.Empty,
            ChannelTitle: WebUtility.HtmlDecode(video.Snippet?.ChannelTitle ?? string.Empty));

    private static YoutubeVideo MapToYoutubeVideo(Google.Apis.YouTube.v3.Data.SearchResult item) =>
        new(
            VideoId: item.Id.VideoId ?? string.Empty,
            Title: WebUtility.HtmlDecode(item.Snippet?.Title ?? string.Empty),
            Description: WebUtility.HtmlDecode(item.Snippet?.Description ?? string.Empty),
            ImageUrl: item.Snippet?.Thumbnails?.Default__?.Url ?? string.Empty,
            ChannelTitle: WebUtility.HtmlDecode(item.Snippet?.ChannelTitle ?? string.Empty));

    public async Task<YoutubeClientResponse> DownloadVideoAsync(string videoId, string outputPath,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(outputPath);

        var videoUrl = $"https://www.youtube.com/watch?v={videoId}";
        var outputTemplate = Path.Combine(outputPath, $"{Guid.NewGuid():N}.%(ext)s");

        var options = new OptionSet
        {
            NoPlaylist = true,
            ExtractAudio = true,
            AudioFormat = AudioConversionFormat.Mp3,
            EmbedThumbnail = true,
            EmbedMetadata = true,
            ExtractorArgs = "youtube:player_client=android_vr",
            Output = outputTemplate,
            // Precedence: cookiesFromBrowser wins over cookiesFile.
            // When both are set, only pass --cookies-from-browser to avoid conflicting flags.
            CookiesFromBrowser = !string.IsNullOrEmpty(cookiesFromBrowser) ? cookiesFromBrowser : null,
            Cookies = string.IsNullOrEmpty(cookiesFromBrowser) && !string.IsNullOrEmpty(cookiesFile) ? cookiesFile : null,
            Proxy = string.IsNullOrEmpty(proxy) ? null : proxy,
        };
        await _youtubeDlLock.WaitAsync(cancellationToken);
        try
        {
            var result = await _youtubeDl.RunWithOptions(videoUrl, options, ct: cancellationToken);

            if (!result.Success)
            {
                options.ExtractorArgs = null;
                options.Output = outputTemplate;
                var fallbackResult = await _youtubeDl.RunWithOptions(videoUrl, options, ct: cancellationToken);
                if (fallbackResult.Success)
                    result = fallbackResult;
            }

            if (!result.Success)
                return new YoutubeClientResponse(null, false, result.ErrorOutput ?? []);

            var resolvedPath = result.Data;
            if (string.IsNullOrEmpty(resolvedPath) || !File.Exists(resolvedPath))
            {
                resolvedPath = Directory.GetFiles(outputPath, "*.mp3")
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .FirstOrDefault();
            }

            return new YoutubeClientResponse(resolvedPath, !string.IsNullOrEmpty(resolvedPath), []);
        }
        finally
        {
            _youtubeDlLock.Release();
        }
    }

    private static void ValidateVideoId(string videoId)
    {
        if (string.IsNullOrWhiteSpace(videoId) || !Regex.IsMatch(videoId, @"^[a-zA-Z0-9_-]{11}$"))
            throw new ArgumentException($"Invalid videoId: '{videoId}'", nameof(videoId));
    }

    public Process BuildPreviewAudioProcess(string videoId)
    {
        ValidateVideoId(videoId);

        var videoUrl = $"https://www.youtube.com/watch?v={videoId}";

        var startInfo = new ProcessStartInfo(ytDlpPath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("--no-playlist");
        // No --extractor-args override: the default (web) client picks up the PO token
        // from /etc/yt-dlp.conf and streams through WARP, whereas android_vr produces
        // format URLs that 403 even through the proxy with no fallback in the single-shot
        // streaming preview.
        startInfo.ArgumentList.Add("-f");
        startInfo.ArgumentList.Add("bestaudio[ext=webm]/bestaudio");
        if (!string.IsNullOrEmpty(cookiesFromBrowser))
        {
            startInfo.ArgumentList.Add("--cookies-from-browser");
            startInfo.ArgumentList.Add(cookiesFromBrowser);
        }
        else if (!string.IsNullOrEmpty(cookiesFile))
        {
            startInfo.ArgumentList.Add("--cookies");
            startInfo.ArgumentList.Add(cookiesFile);
        }
        if (!string.IsNullOrEmpty(proxy))
        {
            startInfo.ArgumentList.Add("--proxy");
            startInfo.ArgumentList.Add(proxy);
        }
        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add("-");
        startInfo.ArgumentList.Add(videoUrl);

        return new Process { StartInfo = startInfo };
    }

    public void Dispose()
    {
        _youTubeService.Dispose();
        _youtubeDlLock.Dispose();
    }
}