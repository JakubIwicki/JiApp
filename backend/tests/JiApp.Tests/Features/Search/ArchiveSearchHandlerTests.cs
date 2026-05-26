using System.Threading.Tasks;
using FluentAssertions;
using JiApp.Api.Features.Search.ArchiveSearch;
using JiApp.Tests.Fixtures;
using Xunit;

namespace JiApp.Tests.Features.Search;

public class ArchiveSearchHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidId_ReturnsSuccess()
    {
        var ctx = new ArchiveSearchHandlerFixture()
            .WithArchiveAsync(1L, 1L)
            .Build();

        var request = new ArchiveSearchRequest(1);

        var result = await ctx.Handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentId_ReturnsFailure()
    {
        var ctx = new ArchiveSearchHandlerFixture()
            .WithArchiveAsyncNotFound(99L, 1L)
            .Build();

        var request = new ArchiveSearchRequest(99);

        var result = await ctx.Handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }
}