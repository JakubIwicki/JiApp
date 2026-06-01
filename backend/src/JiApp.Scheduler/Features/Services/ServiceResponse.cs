using JiApp.Scheduler.Features.Common;

namespace JiApp.Scheduler.Features.Services;

[Serializable]
public sealed record ServiceResponse(
    long Id,
    long BoardId,
    string Name,
    string Category,
    int BaseDuration,
    PriceResponse BasePrice);