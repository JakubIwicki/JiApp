namespace api.JiApp.LovingBoards.Clients;

/// <summary>
/// Development-only fallback that always reports the user as found. Used when no
/// IdentityBaseUrl is configured in a Development environment — mirrors
/// <see cref="JiApp.Common.Services.NoOpSecurityStampValidator"/>.
/// </summary>
public sealed class NoOpUserExistenceClient : IUserExistenceClient
{
    public Task<UserExistenceStatus> CheckExistsAsync(long userId, CancellationToken ct)
        => Task.FromResult(UserExistenceStatus.Found);
}
