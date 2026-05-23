using System;
using System.Collections.Generic;
using JiApp.Api.Features.History.GetHistory;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Infrastructure.Repositories;
using JiApp.Tests.Mocks;
using Moq;

namespace JiApp.Tests.Fixtures;

public sealed class GetHistoryHandlerFixture
{
    private readonly Mock<ISearchHistoryRepository>
        _searchHistoryRepoMock = SearchHistoryRepositoryMock.GetSuccessful();

    private readonly Mock<IDownloadHistoryRepository> _downloadHistoryRepoMock =
        DownloadHistoryRepositoryMock.GetSuccessful();

    private readonly Mock<ICurrentUserService> _currentUserServiceMock = CurrentUserServiceMock.GetSuccessful();

    public GetHistoryHandlerFixture WithSearchGetByUserIdAsync(long userId, int limit,
        IReadOnlyList<YoutubeSearchHistory> result, int offset = 0)
    {
        _searchHistoryRepoMock.Setup(x => x.GetByUserIdAsync(userId, limit, offset)).ReturnsAsync(result);
        return this;
    }

    public GetHistoryHandlerFixture WithSearchGetByUserIdAsync_Throws(long userId, int limit, Exception ex,
        int offset = 0)
    {
        _searchHistoryRepoMock.Setup(x => x.GetByUserIdAsync(userId, limit, offset)).ThrowsAsync(ex);
        return this;
    }

    public GetHistoryHandlerFixture WithDownloadGetByUserIdAsync(long userId, int limit,
        IReadOnlyList<YoutubeDownloadHistory> result, int offset = 0)
    {
        _downloadHistoryRepoMock.Setup(x => x.GetByUserIdAsync(userId, limit, offset)).ReturnsAsync(result);
        return this;
    }

    public GetHistoryHandlerFixture WithDownloadGetByUserIdAsync_Throws(long userId, int limit, Exception ex,
        int offset = 0)
    {
        _downloadHistoryRepoMock.Setup(x => x.GetByUserIdAsync(userId, limit, offset)).ThrowsAsync(ex);
        return this;
    }

    public GetHistoryHandlerContext Build()
    {
        var handler = new GetHistoryHandler(
            _searchHistoryRepoMock.Object,
            _downloadHistoryRepoMock.Object,
            _currentUserServiceMock.Object,
            LoggerMock.Of<GetHistoryHandler>());

        return new GetHistoryHandlerContext(handler);
    }
}

public sealed record GetHistoryHandlerContext(
    GetHistoryHandler Handler);