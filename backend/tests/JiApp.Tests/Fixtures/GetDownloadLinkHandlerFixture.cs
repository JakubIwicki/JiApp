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
    private readonly Mock<IYoutubeClient> _youtubeClientMock;
    private readonly Mock<ITempFileStore> _tempFileStoreMock;
    private readonly Mock<IDownloadHistoryRepository> _downloadHistoryRepoMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Settings _settings;

    public GetDownloadLinkHandlerFixture()
    {
        _youtubeClientMock = YoutubeClientMock.GetSuccessful();
        _tempFileStoreMock = TempFileStoreMock.GetSuccessful();
        _downloadHistoryRepoMock = DownloadHistoryRepositoryMock.GetSuccessful();
        _currentUserServiceMock = CurrentUserServiceMock.GetWithUsername(1L, "testuser");
        _settings = new Settings
        {
            App = new Settings.AppSettings
            {
                BaseDirectory = "/tmp/ji_app"
            }
        };
    }

    public GetDownloadLinkHandlerFixture WithDownloadVideoAsync(string videoId, string outputFolder, YoutubeClientResponse result)
    {
        _youtubeClientMock.Setup(x => x.DownloadVideoAsync(videoId, outputFolder)).ReturnsAsync(result);
        return this;
    }

    public GetDownloadLinkHandlerFixture WithAnyDownloadVideoAsync(YoutubeClientResponse result)
    {
        _youtubeClientMock.Setup(x => x.DownloadVideoAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(result);
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
            _youtubeClientMock,
            _tempFileStoreMock,
            _downloadHistoryRepoMock,
            _currentUserServiceMock);
    }
}

public sealed record GetDownloadLinkHandlerContext(
    GetDownloadLinkHandler Handler,
    Mock<IYoutubeClient> YoutubeClientMock,
    Mock<ITempFileStore> TempFileStoreMock,
    Mock<IDownloadHistoryRepository> DownloadHistoryRepoMock,
    Mock<ICurrentUserService> CurrentUserServiceMock);
