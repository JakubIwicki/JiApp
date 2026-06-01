namespace JiApp.ImageTools.Configuration;

[Serializable]
public sealed class ImageToolsSettings
{
    public void Validate()
    {
        // All configuration validation is handled upstream by Gateway.
        // ImageTools has no service-specific config to validate.
    }
}