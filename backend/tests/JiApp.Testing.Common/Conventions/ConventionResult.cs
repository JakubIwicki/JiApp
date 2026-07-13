namespace JiApp.Testing.Common.Conventions;

public readonly record struct ConventionResult(List<string> Violations, int ScannedCount);
