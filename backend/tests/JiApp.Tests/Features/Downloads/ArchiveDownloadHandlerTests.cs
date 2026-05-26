using System.Threading.Tasks;
using FluentAssertions;
using JiApp.Api.Features.Downloads.ArchiveDownload;
using JiApp.Tests.Fixtures;
using Xunit;

namespace JiApp.Tests.Features.Downloads;

public class ArchiveDownloadHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidId_ReturnsSuccess()
    {
        var ctx = new ArchiveDownloadHandlerFixture()
            .WithArchiveAsync(1L, 1L)
            .Build();

        var request = new ArchiveDownloadRequest(1);

        var result = await ctx.Handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentId_ReturnsFailure()
    {
        var ctx = new ArchiveDownloadHandlerFixture()
            .WithArchiveAsyncNotFound(99L, 1L)
            .Build();

        var request = new ArchiveDownloadRequest(99);

        var result = await ctx.Handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }
}