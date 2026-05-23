using JiApp.Api.Logging;
using JiApp.Common.Abstractions;
using JiApp.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace JiApp.Api.Features.Downloads.DownloadFile;

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
