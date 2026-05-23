using FluentAssertions;
using JiApp.Api.Features.Downloads.DownloadFile;
using JiApp.Tests.Fixtures;
using Xunit;

namespace JiApp.Tests.Features.Downloads;

public class DownloadFileHandlerTests
{
    [Fact]
    public void Handle_WithValidIdAndOwnedFile_ReturnsFilePath()
    {
        var ctx = new DownloadFileHandlerFixture()
            .WithGet("valid-id", 1L, "/tmp/ji_app/YtMp3_1/song.mp3")
            .Build();

        var result = ctx.Handler.Handle("valid-id");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/tmp/ji_app/YtMp3_1/song.mp3");
    }

    [Fact]
    public void Handle_WithExpiredId_ReturnsFailure()
    {
        var ctx = new DownloadFileHandlerFixture()
            .WithGet("expired-id", 1L, null)
            .Build();

        var result = ctx.Handler.Handle("expired-id");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Handle_WhenFileNotOwnedByUser_ReturnsFailure()
    {
        var ctx = new DownloadFileHandlerFixture()
            .WithGet("other-user-file", 1L, null)
            .Build();

        var result = ctx.Handler.Handle("other-user-file");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }
}
