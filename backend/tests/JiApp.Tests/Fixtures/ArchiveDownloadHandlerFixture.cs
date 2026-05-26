using JiApp.Api.Features.Downloads.ArchiveDownload;
using JiApp.Common.Abstractions;
using JiApp.Infrastructure.Repositories;
using JiApp.Tests.Mocks;
using Moq;

namespace JiApp.Tests.Fixtures;

public sealed class ArchiveDownloadHandlerFixture
{
    private readonly Mock<IDownloadHistoryRepository> _downloadHistoryRepoMock =
        DownloadHistoryRepositoryMock.GetSuccessful();

    private readonly Mock<ICurrentUserService> _currentUserServiceMock = CurrentUserServiceMock.GetSuccessful();

    public ArchiveDownloadHandlerFixture WithArchiveAsync(long id, long userId)
    {
        _downloadHistoryRepoMock.Setup(x => x.ArchiveAsync(id, userId)).ReturnsAsync(true);
        return this;
    }

    public ArchiveDownloadHandlerFixture WithArchiveAsyncNotFound(long id, long userId)
    {
        _downloadHistoryRepoMock.Setup(x => x.ArchiveAsync(id, userId)).ReturnsAsync(false);
        return this;
    }

    public ArchiveDownloadHandlerContext Build()
    {
        var handler = new ArchiveDownloadHandler(
            _downloadHistoryRepoMock.Object,
            _currentUserServiceMock.Object);

        return new ArchiveDownloadHandlerContext(handler);
    }
}

public sealed record ArchiveDownloadHandlerContext(
    ArchiveDownloadHandler Handler);