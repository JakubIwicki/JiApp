using JiApp.Scheduler.Features.Common;

namespace JiApp.Scheduler.Features.Services.UpdateService;

[Serializable]
public sealed record UpdateServiceRequest(
    string Name,
    string Category,
    int BaseDuration,
    PriceRequest BasePrice);