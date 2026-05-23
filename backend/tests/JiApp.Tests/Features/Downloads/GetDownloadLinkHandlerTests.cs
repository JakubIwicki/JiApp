using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using JiApp.Api.Configuration;
using JiApp.Api.Features.Downloads.GetDownloadLink;
using JiApp.Common.Models;
using JiApp.Infrastructure.Repositories;
using JiApp.Infrastructure.Services;
using JiApp.Tests.Mocks;
using JiApp.YtApi;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace JiApp.Tests.Features.Downloads;

public class GetDownloadLinkHandlerTests
{
    private readonly Mock<IYoutubeClient> _youtubeClientMock;
    private readonly Mock<ITempFileStore> _tempFileStoreMock;
    private readonly Mock<IDownloadHistoryRepository> _downloadHistoryRepoMock;
    private readonly GetDownloadLinkHandler _handler;

    public GetDownloadLinkHandlerTests()
    {
        _youtubeClientMock = YoutubeClientMock.GetSuccessful();
        _tempFileStoreMock = TempFileStoreMock.GetSuccessful();
        _downloadHistoryRepoMock = DownloadHistoryRepositoryMock.GetSuccessful();
        var currentUserMock = CurrentUserServiceMock.GetWithUsername(1L, "testuser");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["App:BaseDirectory"] = "/tmp/ji_app"
            })
            .Build();
        var settings = new Settings();
        config.Bind(settings);

        var loggerMock = LoggerMock.GetSuccessful<GetDownloadLinkHandler>();

        _handler = new GetDownloadLinkHandler(
            _youtubeClientMock.Object,
            _tempFileStoreMock.Object,
            _downloadHistoryRepoMock.Object,
            currentUserMock.Object,
            settings,
            loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidRequest_ReturnsTempId()
    {
        const string downloadedFilePath = "/tmp/ji_app/YtMp3_1/song.mp3";
        const string tempFileId = "abc123def456";

        _youtubeClientMock.Setup(x => x.DownloadVideoAsync(
                It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new YoutubeClientResponse(downloadedFilePath, true, []));

        _tempFileStoreMock.Setup(x => x.Add(downloadedFilePath, 1L))
            .Returns(tempFileId);

        var request = new DownloadRequest(
            "dQw4w9WgXcQ",
            "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            "Test Title",
            "Test Description",
            "https://img.url/thumb.jpg");


        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.TempId.Should().Be(tempFileId);
    }

    [Fact]
    public async Task HandleAsync_WhenYtDlpFails_ReturnsFailure()
    {
        _youtubeClientMock.Setup(x => x.DownloadVideoAsync(
                It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new YoutubeClientResponse(null, false, ["Download failed"]));

        var request = new DownloadRequest(
            "dQw4w9WgXcQ",
            "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            null, null, null);


        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task HandleAsync_SavesDownloadHistory_OnSuccess()
    {
        const string downloadedFilePath = "/tmp/ji_app/YtMp3_1/song.mp3";
        const string tempFileId = "abc123def456";

        _youtubeClientMock.Setup(x => x.DownloadVideoAsync(
                It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new YoutubeClientResponse(downloadedFilePath, true, []));

        _tempFileStoreMock.Setup(x => x.Add(downloadedFilePath, 1L))
            .Returns(tempFileId);

        var request = new DownloadRequest(
            "dQw4w9WgXcQ",
            "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            "Test Title",
            "Test Description",
            "https://img.url/thumb.jpg");


        await _handler.HandleAsync(request);

        _downloadHistoryRepoMock.Verify(x => x.AddAsync(It.Is<YoutubeDownloadHistory>(h =>
            h.UserId == 1L &&
            h.VideoId == "dQw4w9WgXcQ" &&
            h.VideoTitle == "Test Title" &&
            h.VideoDescription == "Test Description" &&
            h.VideoUrl == "https://www.youtube.com/watch?v=dQw4w9WgXcQ" &&
            h.ImageUrl == "https://img.url/thumb.jpg" &&
            h.DownloadedAt.Kind == DateTimeKind.Utc)), Times.Once);
    }
}
