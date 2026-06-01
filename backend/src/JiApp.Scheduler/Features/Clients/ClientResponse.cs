namespace JiApp.Scheduler.Features.Clients;

[Serializable]
public sealed record ClientResponse(long Id, long BoardId, string Name, string? Phone, string? Notes);