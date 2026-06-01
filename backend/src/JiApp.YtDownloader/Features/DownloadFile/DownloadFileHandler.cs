using JiApp.Common.Abstractions;
using JiApp.YtDownloader.Logging;
using JiApp.YtDownloader.Services;
using Microsoft.Extensions.Logging;

namespace JiApp.YtDownloader.Features.DownloadFile;

public sealed class DownloadFileHandler(
    ITempFileStore tempFileStore,
    ICurrentUserService currentUser,
    ILogger<DownloadFileHandler> logger)
{
    public Result<string> Handle(string id)
    {
        logger.DownloadRequestedForFile(id);

        var filePath = tempFileStore.Get(id, currentUser.UserId);

        if (filePath is null)
        {
            logger.FileExpiredOrNotFound(id, currentUser.UserId);
            return Result<string>.Failure("File expired or not found");
        }

        return Result<string>.Success(filePath);
    }
}
