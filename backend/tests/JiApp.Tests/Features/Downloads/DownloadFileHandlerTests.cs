using FluentAssertions;
using JiApp.Api.Features.Downloads.DownloadFile;
using JiApp.Infrastructure.Services;
using JiApp.Tests.Mocks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JiApp.Tests.Features.Downloads;

public class DownloadFileHandlerTests
{
    private readonly Mock<ITempFileStore> _tempFileStoreMock;
    private readonly DownloadFileHandler _handler;

    public DownloadFileHandlerTests()
    {
        _tempFileStoreMock = TempFileStoreMock.GetSuccessful();
        var currentUserMock = CurrentUserServiceMock.GetSuccessful();
        var loggerMock = LoggerMock.GetSuccessful<DownloadFileHandler>();
        _handler = new DownloadFileHandler(_tempFileStoreMock.Object, currentUserMock.Object, loggerMock.Object);
    }

    [Fact]
    public void Handle_WithValidIdAndOwnedFile_ReturnsFilePath()
    {
        _tempFileStoreMock.Setup(x => x.Get("valid-id", 1L))
            .Returns("/tmp/ji_app/YtMp3_1/song.mp3");

        var result = _handler.Handle("valid-id");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/tmp/ji_app/YtMp3_1/song.mp3");
    }

    [Fact]
    public void Handle_WithExpiredId_ReturnsFailure()
    {
        _tempFileStoreMock.Setup(x => x.Get("expired-id", 1L))
            .Returns((string?)null);

        var result = _handler.Handle("expired-id");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Handle_WhenFileNotOwnedByUser_ReturnsFailure()
    {
        _tempFileStoreMock.Setup(x => x.Get("other-user-file", 1L))
            .Returns((string?)null);

        var result = _handler.Handle("other-user-file");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }
}
