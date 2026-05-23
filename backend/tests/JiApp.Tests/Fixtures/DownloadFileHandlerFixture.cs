using JiApp.Api.Features.Downloads.DownloadFile;
using JiApp.Common.Abstractions;
using JiApp.Infrastructure.Services;
using JiApp.Tests.Mocks;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Tests.Fixtures;

public sealed class DownloadFileHandlerFixture
{
    private readonly Mock<ITempFileStore> _tempFileStoreMock = TempFileStoreMock.GetSuccessful();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = CurrentUserServiceMock.GetSuccessful();

    public DownloadFileHandlerFixture WithGet(string id, long userId, string? filePath)
    {
        _tempFileStoreMock.Setup(x => x.Get(id, userId)).Returns(filePath);
        return this;
    }

    public DownloadFileHandlerContext Build()
    {
        var handler = new DownloadFileHandler(
            _tempFileStoreMock.Object,
            _currentUserServiceMock.Object,
            LoggerMock.Of<DownloadFileHandler>());

        return new DownloadFileHandlerContext(handler);
    }
}

public sealed record DownloadFileHandlerContext(
    DownloadFileHandler Handler);