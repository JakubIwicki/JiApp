namespace JiApp.ImageTools.Tests;

public class ImageToolsSettingsTests
{
    [Fact]
    public void Validate_does_not_throw_when_no_settings_configured()
    {
        var sut = new ImageToolsSettings();

        var act = () => sut.Validate();

        act.Should().NotThrow();
    }
}