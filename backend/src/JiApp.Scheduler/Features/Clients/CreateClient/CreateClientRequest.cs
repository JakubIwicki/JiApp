namespace JiApp.Scheduler.Features.Clients.CreateClient;

[Serializable]
public sealed record CreateClientRequest(long BoardId, string Name, string? Phone, string? Notes);