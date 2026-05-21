using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Infrastructure.Repositories;
using JiApp.Infrastructure.Services;
using JiApp.YtApi;

namespace JiApp.Api.Features.Downloads.GetDownloadLink;

public sealed class GetDownloadLinkHandler(
    IYoutubeClient youtubeClient,
    ITempFileStore tempFileStore,
    IDownloadHistoryRepository downloadHistoryRepository,
    ICurrentUserService currentUser,
    IConfiguration configuration)
{
    public async Task<Result<DownloadResponse>> HandleAsync(DownloadRequest request)
    {
        var baseDirectory = configuration["App:BaseDirectory"]
            ?? throw new InvalidOperationException("App:BaseDirectory is not configured");

        // Use userId instead of username to prevent path traversal
        var outputFolder = Path.Combine(baseDirectory, $"YtMp3_{currentUser.UserId}");

        YoutubeClientResponse downloadResult;
        try
        {
            downloadResult = await youtubeClient.DownloadVideoAsync(
                request.VideoUrl, outputFolder);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result<DownloadResponse>.Failure(
                $"Failed to process download: {ex.Message}");
        }

        if (!downloadResult.Success)
        {
            var errors = string.Join(", ", downloadResult.Errors);
            return Result<DownloadResponse>.Failure(
                $"Failed to download video: {errors}");
        }

        var filePath = downloadResult.FilePath!;
        var tempId = tempFileStore.Add(filePath);

        var historyEntry = new YoutubeDownloadHistory
        {
            UserId = currentUser.UserId,
            DownloadedAt = DateTime.UtcNow,
            VideoTitle = request.Title,
            VideoDescription = request.Description,
            VideoId = request.VideoId,
            VideoUrl = request.VideoUrl,
            ImageUrl = request.ImageUrl
        };

        await downloadHistoryRepository.AddAsync(historyEntry);
        await downloadHistoryRepository.SaveChangesAsync();

        return Result<DownloadResponse>.Success(new DownloadResponse(tempId, string.Empty));
    }
}
