namespace api.JiApp.LovingBoards.Configuration;

[Serializable]
public sealed class LovingBoardsSettings
{
    public string? ConnectionString { get; set; }
    public JwtSettings? Jwt { get; set; }
    public string[]? CorsAllowedOrigins { get; set; }
    public string? IdentityBaseUrl { get; set; }
    public int MaxBoardsPerUser { get; set; } = 50;
    public int DefaultPageSize { get; set; } = 50;
    public int MaxBoardNameLength { get; set; } = 200;
    public int MaxItemsPerBoard { get; set; } = 200;
    public int MaxItemTitleLength { get; set; } = 200;
    public int MaxQuantityLength { get; set; } = 50;
    public int MaxCategoryLength { get; set; } = 100;
    public int MaxNoteLength { get; set; } = 1000;

    public void Validate()
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(ConnectionString))
            errors.Add("ConnectionString is required");

        if (Jwt is null)
            errors.Add("Jwt section is required");
        else
            Jwt.Validate();

        if (errors.Count > 0)
            throw new InvalidOperationException(
                $"LovingBoardsSettings validation failed: {string.Join("; ", errors)}");
    }
}

[Serializable]
public sealed class JwtSettings
{
    public string? Key { get; set; }
    public string? Issuer { get; set; }
    public string? Audience { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Key))
            throw new InvalidOperationException("Jwt:Key is required");
        if (Key is not null && Key.Length < 32)
            throw new InvalidOperationException("Jwt:Key must be at least 32 characters long.");
        if (string.IsNullOrWhiteSpace(Issuer))
            throw new InvalidOperationException("Jwt:Issuer is required");
        if (string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException("Jwt:Audience is required");
    }
}
