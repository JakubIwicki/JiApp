using JiApp.Common.Abstractions;
using JiApp.YtDownloader.Logging;
using JiApp.YtDownloader.Repositories;
using Microsoft.Extensions.Logging;

namespace JiApp.YtDownloader.Features.DownloadHistory;

public sealed class DownloadHistoryHandler(
    IDownloadHistoryRepository downloadHistoryRepository,
    ICurrentUserService currentUser,
    ILogger<DownloadHistoryHandler> logger)
{
    public async Task<Result<DownloadHistoryResponse>> HandleAsync(DownloadHistoryRequest request)
    {
        var limit = request.Limit ?? 10;

        logger.FetchingDownloadHistory(limit);

        var history = await downloadHistoryRepository.GetByUserIdAsync(
            currentUser.UserId, limit);

        var items = history.Select(DownloadHistoryItem.FromEntity).ToList();

        return Result<DownloadHistoryResponse>.Success(
            new DownloadHistoryResponse(items.AsReadOnly()));
    }
}
