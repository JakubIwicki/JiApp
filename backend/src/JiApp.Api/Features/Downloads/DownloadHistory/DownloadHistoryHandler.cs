using System.Linq;
using System.Threading.Tasks;
using JiApp.Api.Logging;
using JiApp.Common.Abstractions;
using JiApp.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace JiApp.Api.Features.Downloads.DownloadHistory;

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

        var items = history.Select(h => new DownloadHistoryItem(
            h.Id,
            h.VideoTitle,
            h.VideoDescription,
            h.VideoId ?? string.Empty,
            h.VideoUrl ?? string.Empty,
            h.ImageUrl,
            h.DownloadedAt
        )).ToList();

        return Result<DownloadHistoryResponse>.Success(
            new DownloadHistoryResponse(items.AsReadOnly()));
    }
}
