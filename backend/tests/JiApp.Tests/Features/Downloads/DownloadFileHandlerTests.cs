using FluentAssertions;
using JiApp.Api.Features.Downloads.DownloadFile;
using JiApp.Common.Abstractions;
using JiApp.Infrastructure.Services;
using Moq;

namespace JiApp.Tests.Features.Downloads;

public class DownloadFileHandlerTests
{
    private readonly Mock<ITempFileStore> _tempFileStoreMock;
    private readonly DownloadFileHandler _handler;

    public DownloadFileHandlerTests()
    {
        _tempFileStoreMock = new Mock<ITempFileStore>();
        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(x => x.UserId).Returns(1L);
        _handler = new DownloadFileHandler(_tempFileStoreMock.Object, currentUserMock.Object);
    }

    [Fact]
    public void Handle_WithValidId_ReturnsFilePath()
    {
        _tempFileStoreMock.Setup(x => x.Get("valid-id"))
            .Returns("/tmp/ji_app/YtMp3_1/song.mp3");

        var result = _handler.Handle("valid-id");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/tmp/ji_app/YtMp3_1/song.mp3");
    }

    [Fact]
    public void Handle_WithExpiredId_ReturnsFailure()
    {
        _tempFileStoreMock.Setup(x => x.Get("expired-id"))
            .Returns((string?)null);

        var result = _handler.Handle("expired-id");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Handle_WhenFileNotInUserDirectory_ReturnsFailure()
    {
        _tempFileStoreMock.Setup(x => x.Get("other-user-id"))
            .Returns("/tmp/ji_app/YtMp3_2/song.mp3");

        var result = _handler.Handle("other-user-id");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }
}
