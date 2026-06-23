namespace JiApp.ImageTools.Tests;

public sealed class ImageToolsSmokeTests
{
    [Fact]
    public void Test_project_is_loadable()
    {
        var type = typeof(JiApp.ImageTools.Program);
        type.Should().NotBeNull();
        type.Assembly.FullName.Should().Contain("JiApp.ImageTools");
    }
}