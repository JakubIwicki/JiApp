namespace JiApp.ImageTools.Tests;

public sealed class ImageToolsSettingsTests
{
    [Fact]
    public void Validate_DoesNotThrow_WhenNoSettingsConfigured()
    {
        var sut = new ImageToolsSettings();

        var act = () => sut.Validate();

        act.Should().NotThrow();
    }
}