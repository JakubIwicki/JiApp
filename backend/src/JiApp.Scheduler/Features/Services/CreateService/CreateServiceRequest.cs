using JiApp.Scheduler.Features.Common;

namespace JiApp.Scheduler.Features.Services.CreateService;

[Serializable]
public sealed record CreateServiceRequest(
    long BoardId,
    string Name,
    string Category,
    int BaseDuration,
    PriceRequest BasePrice);