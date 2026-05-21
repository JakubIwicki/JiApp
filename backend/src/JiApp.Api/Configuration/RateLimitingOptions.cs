namespace JiApp.Api.Configuration;

public class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public RateLimitPolicyOptions Login { get; set; } = new();
    public RateLimitPolicyOptions Register { get; set; } = new();
    public RateLimitPolicyOptions Health { get; set; } = new();
    public RateLimitPolicyOptions DownloadFile { get; set; } = new();
}

public class RateLimitPolicyOptions
{
    public int PermitLimit { get; init; } = 10;
    public int WindowInSeconds { get; init; } = 60;
    public int QueueLimit { get; init; } = 0;
    public int SegmentsPerWindow { get; init; } = 4;
}

public static class RateLimitPolicyNames
{
    public const string Login = "LoginPolicy";
    public const string Register = "RegisterPolicy";
    public const string Health = "HealthPolicy";
    public const string DownloadFile = "DownloadFilePolicy";
}
