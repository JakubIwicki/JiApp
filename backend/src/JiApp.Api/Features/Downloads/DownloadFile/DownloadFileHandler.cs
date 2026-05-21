using JiApp.Common.Abstractions;
using JiApp.Infrastructure.Services;

namespace JiApp.Api.Features.Downloads.DownloadFile;

public sealed class DownloadFileHandler
{
    private readonly ITempFileStore _tempFileStore;
    private readonly ICurrentUserService _currentUser;

    public DownloadFileHandler(ITempFileStore tempFileStore, ICurrentUserService currentUser)
    {
        _tempFileStore = tempFileStore;
        _currentUser = currentUser;
    }

    public Result<string> Handle(string id)
    {
        var filePath = _tempFileStore.Get(id);

        if (filePath is null)
            return Result<string>.Failure("File expired or not found");

        // Verify the file belongs to the requesting user by checking the output directory
        var expectedDir = $"{Path.DirectorySeparatorChar}YtMp3_{_currentUser.UserId}{Path.DirectorySeparatorChar}";
        if (!filePath.Contains(expectedDir))
            return Result<string>.Failure("File not found");

        return Result<string>.Success(filePath);
    }
}
