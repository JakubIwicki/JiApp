using JiApp.Common.Abstractions;
using JiApp.YtDownloader.Logging;
using JiApp.YtDownloader.Services;
using Microsoft.Extensions.Logging;

namespace JiApp.YtDownloader.Features.DownloadFile;

public sealed class DownloadFileHandler(
    IDownloadJobStore jobStore,
    ICurrentUserService currentUser,
    ILogger<DownloadFileHandler> logger)
{
    public Result<string> Handle(string id)
    {
        logger.DownloadRequestedForFile(id);

        var filePath = jobStore.GetFilePath(id, currentUser.UserId);

        if (filePath is null)
        {
            logger.FileExpiredOrNotFound(id, currentUser.UserId);
            return Result<string>.Failure("File expired or not found", ResultCategories.NotFound);
        }

        return Result<string>.Success(filePath);
    }
}
