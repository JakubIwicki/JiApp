namespace JiApp.Gateway.HealthDashboard;

public sealed record HealthCheckResult(string Name, string Url, string Status, string Color);