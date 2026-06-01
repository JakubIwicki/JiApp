namespace JiApp.Scheduler.Features.Clients.UpdateClient;

[Serializable]
public sealed record UpdateClientRequest(string Name, string? Phone, string? Notes);
