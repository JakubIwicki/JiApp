namespace JiApp.Gateway.Tests.Integration;

/// <summary>
/// Full-pipeline host for the Gateway. Derives from the shared
/// <see cref="IntegrationTestBase{TEntryPoint}"/>, which sets the "Test"
/// environment so appsettings.Test.json loads (provides Jwt:Key) instead of
/// appsettings.Development.json (which references a Kestrel dev cert path that
/// doesn't exist in the test runner environment) and applies the WSL inotify
/// polling workaround.
/// </summary>
public class GatewayWebApplicationFactory : IntegrationTestBase<JiApp.Gateway.Program>
{
}
