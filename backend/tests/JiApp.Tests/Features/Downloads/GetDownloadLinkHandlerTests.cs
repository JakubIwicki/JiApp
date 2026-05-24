using System;
using System.Threading.Tasks;
using FluentAssertions;
using JiApp.Api.Features.Downloads.GetDownloadLink;
using JiApp.Common.Models;
using JiApp.Tests.Fixtures;
using JiApp.YtApi;
using Moq;
using Xunit;

namespace JiApp.Tests.Features.Downloads;

public class GetDownloadLinkHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidRequest_ReturnsTempId()
    {
        const string downloadedFilePath = "/tmp/ji_app/YtMp3_1/song.mp3";
        const string tempFileId = "abc123def456";

        Directory.CreateDirectory("/tmp/ji_app/YtMp3_1");
        File.WriteAllText(downloadedFilePath, "test content");
        try
        {
            var ctx = new GetDownloadLinkHandlerFixture()
                .WithAnyDownloadVideoAsync(new YoutubeClientResponse(downloadedFilePath, true, []))
                .WithTempFileStoreAdd(downloadedFilePath, 1L, tempFileId)
                .Build();

            var request = new DownloadRequest(
                "dQw4w9WgXcQ",
                "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                "Test Title",
                "Test Description",
                "https://img.url/thumb.jpg");

            var result = await ctx.Handler.HandleAsync(request);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.TempId.Should().Be(tempFileId);
        }
        finally
        {
            if (File.Exists(downloadedFilePath))
                File.Delete(downloadedFilePath);
        }
    }

    [Fact]
    public async Task HandleAsync_WhenYtDlpFails_ReturnsFailure()
    {
        var ctx = new GetDownloadLinkHandlerFixture()
            .WithAnyDownloadVideoAsync(new YoutubeClientResponse(null, false, ["Download failed"]))
            .Build();

        var request = new DownloadRequest(
            "dQw4w9WgXcQ",
            "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            null, null, null);

        var result = await ctx.Handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
        result.ErrorCategory.Should().Be("YoutubeDl");
    }

    [Fact]
    public async Task HandleAsync_SavesDownloadHistory_OnSuccess()
    {
        const string downloadedFilePath = "/tmp/ji_app/YtMp3_1/song.mp3";
        const string tempFileId = "abc123def456";

        Directory.CreateDirectory("/tmp/ji_app/YtMp3_1");
        File.WriteAllText(downloadedFilePath, "test content");
        try
        {
            var ctx = new GetDownloadLinkHandlerFixture()
                .WithAnyDownloadVideoAsync(new YoutubeClientResponse(downloadedFilePath, true, []))
                .WithTempFileStoreAdd(downloadedFilePath, 1L, tempFileId)
                .Build();

            var request = new DownloadRequest(
                "dQw4w9WgXcQ",
                "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                "Test Title",
                "Test Description",
                "https://img.url/thumb.jpg");

            await ctx.Handler.HandleAsync(request);

            ctx.DownloadHistoryRepoMock.Verify(x => x.AddAsync(It.Is<YoutubeDownloadHistory>(h =>
                h.UserId == 1L &&
                h.VideoId == "dQw4w9WgXcQ" &&
                h.VideoTitle == "Test Title" &&
                h.VideoDescription == "Test Description" &&
                h.VideoUrl == "https://www.youtube.com/watch?v=dQw4w9WgXcQ" &&
                h.ImageUrl == "https://img.url/thumb.jpg" &&
                h.DownloadedAt.Kind == DateTimeKind.Utc)), Times.Once);
        }
        finally
        {
            if (File.Exists(downloadedFilePath))
                File.Delete(downloadedFilePath);
        }
    }

    [Fact]
    public async Task HandleAsync_WhenYoutubeClientThrows_ReturnsFailure()
    {
        var ctx = new GetDownloadLinkHandlerFixture()
            .WithThrowingDownloadVideoAsync(new InvalidOperationException("API failure"))
            .Build();

        var request = new DownloadRequest("dQw4w9WgXcQ", "https://youtube.com/watch?v=dQw4w9WgXcQ", null, null, null);
        var result = await ctx.Handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Failed to process download. Please try again later.");
        result.ErrorCategory.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenDownloadedFileMissing_ReturnsFailure()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}.mp3");
        var mockResponse = new YoutubeClientResponse(nonExistentPath, true, []);

        var ctx = new GetDownloadLinkHandlerFixture()
            .WithAnyDownloadVideoAsync(mockResponse)
            .Build();

        var request = new DownloadRequest("dQw4w9WgXcQ", "https://youtube.com/watch?v=dQw4w9WgXcQ", null, null, null);
        var result = await ctx.Handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("missing");
        result.ErrorCategory.Should().Be(GetDownloadLinkHandler.YoutubeDlErrorCategory);
    }

    [Fact]
    public async Task HandleAsync_WhenDownloadedFileEmpty_ReturnsFailure()
    {
        var emptyFilePath = Path.Combine(Path.GetTempPath(), $"empty_{Guid.NewGuid():N}.mp3");
        File.WriteAllText(emptyFilePath, string.Empty);

        try
        {
            var mockResponse = new YoutubeClientResponse(emptyFilePath, true, []);
            var ctx = new GetDownloadLinkHandlerFixture()
                .WithAnyDownloadVideoAsync(mockResponse)
                .Build();

            var request = new DownloadRequest("dQw4w9WgXcQ", "https://youtube.com/watch?v=dQw4w9WgXcQ", null, null,
                null);
            var result = await ctx.Handler.HandleAsync(request);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("empty");
            result.ErrorCategory.Should().Be(GetDownloadLinkHandler.YoutubeDlErrorCategory);
        }
        finally
        {
            if (File.Exists(emptyFilePath))
                File.Delete(emptyFilePath);
        }
    }
}