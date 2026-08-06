namespace api.JiApp.LovingBoards.Clients;

public enum UserExistenceStatus
{
    Found,
    NotFound,
    Unavailable
}

/// <summary>
/// Probes the Identity service for whether a user exists. Used to guard board
/// membership adds against stale/unknown user ids (G3.5). Fail-closed contract:
/// any transport error is reported as <see cref="UserExistenceStatus.Unavailable"/>
/// so callers never proceed on an unverifiable check.
/// </summary>
public interface IUserExistenceClient
{
    Task<UserExistenceStatus> CheckExistsAsync(long userId, CancellationToken ct);
}
