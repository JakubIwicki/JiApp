using System.Collections.Generic;
using JiApp.Api.Features.Downloads.DownloadHistory;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Infrastructure.Repositories;
using JiApp.Tests.Mocks;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Tests.Fixtures;

public sealed class DownloadHistoryHandlerFixture
{
    private readonly Mock<IDownloadHistoryRepository> _downloadHistoryRepoMock =
        DownloadHistoryRepositoryMock.GetSuccessful();

    private readonly Mock<ICurrentUserService> _currentUserServiceMock = CurrentUserServiceMock.GetSuccessful();

    public DownloadHistoryHandlerFixture WithGetByUserIdAsync(long userId, int limit,
        IReadOnlyList<YoutubeDownloadHistory> result, int offset = 0)
    {
        _downloadHistoryRepoMock.Setup(x => x.GetByUserIdAsync(userId, limit, offset)).ReturnsAsync(result);
        return this;
    }

    public DownloadHistoryHandlerContext Build()
    {
        var handler = new DownloadHistoryHandler(
            _downloadHistoryRepoMock.Object,
            _currentUserServiceMock.Object,
            LoggerMock.Of<DownloadHistoryHandler>());

        return new DownloadHistoryHandlerContext(handler);
    }
}

public sealed record DownloadHistoryHandlerContext(
    DownloadHistoryHandler Handler);