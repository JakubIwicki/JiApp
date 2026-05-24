using JiApp.Api.Configuration;
using JiApp.Api.Features.Downloads.GetDownloadLink;
using JiApp.Common.Abstractions;
using JiApp.Infrastructure.Repositories;
using JiApp.Infrastructure.Services;
using JiApp.Tests.Mocks;
using JiApp.YtApi;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Tests.Fixtures;

public sealed class GetDownloadLinkHandlerFixture
{
    private readonly Mock<IYoutubeClient> _youtubeClientMock = YoutubeClientMock.GetSuccessful();
    private readonly Mock<ITempFileStore> _tempFileStoreMock = TempFileStoreMock.GetSuccessful();

    private readonly Mock<IDownloadHistoryRepository> _downloadHistoryRepoMock =
        DownloadHistoryRepositoryMock.GetSuccessful();

    private readonly Mock<ICurrentUserService> _currentUserServiceMock =
        CurrentUserServiceMock.GetWithUsername(1L, "testuser");

    private readonly Settings _settings = new()
    {
        App = new Settings.AppSettings
        {
            BaseDirectory = "/tmp/ji_app"
        }
    };

    public GetDownloadLinkHandlerFixture WithAnyDownloadVideoAsync(YoutubeClientResponse result)
    {
        _youtubeClientMock.Setup(x =>
                x.DownloadVideoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        return this;
    }

    public GetDownloadLinkHandlerFixture WithThrowingDownloadVideoAsync(Exception exception)
    {
        _youtubeClientMock.Setup(x =>
                x.DownloadVideoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);
        return this;
    }

    public GetDownloadLinkHandlerFixture WithTempFileStoreAdd(string filePath, long userId, string result)
    {
        _tempFileStoreMock.Setup(x => x.Add(filePath, userId)).Returns(result);
        return this;
    }

    public GetDownloadLinkHandlerContext Build()
    {
        var handler = new GetDownloadLinkHandler(
            _youtubeClientMock.Object,
            _tempFileStoreMock.Object,
            _downloadHistoryRepoMock.Object,
            _currentUserServiceMock.Object,
            _settings,
            LoggerMock.Of<GetDownloadLinkHandler>());

        return new GetDownloadLinkHandlerContext(
            handler,
            _downloadHistoryRepoMock);
    }
}

public sealed record GetDownloadLinkHandlerContext(
    GetDownloadLinkHandler Handler,
    Mock<IDownloadHistoryRepository> DownloadHistoryRepoMock);