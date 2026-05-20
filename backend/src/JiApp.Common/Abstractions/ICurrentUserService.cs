namespace JiApp.Common.Abstractions;

public interface ICurrentUserService
{
    long UserId { get; }
    string Username { get; }
}
