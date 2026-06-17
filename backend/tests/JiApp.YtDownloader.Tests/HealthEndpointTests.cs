using JiApp.Common.Abstractions;

namespace JiApp.YtDownloader.Tests;

public class HealthEndpointTests
{
    [Fact]
    public void Test_project_references_resolve()
    {
        true.Should().BeTrue();
    }

    [Fact]
    public void JiApp_Common_abstractions_are_referenceable()
    {
        var error = new ApiErrorResponse(Error: "test");
        error.Error.Should().Be("test");
    }

    [Fact]
    public void YtDownloader_assembly_is_loadable()
    {
        var type = typeof(JiApp.YtDownloader.Program);
        type.Should().NotBeNull();
        type.Assembly.FullName.Should().Contain("JiApp.YtDownloader");
    }
}