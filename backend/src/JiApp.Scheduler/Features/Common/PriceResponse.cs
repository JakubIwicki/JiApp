namespace JiApp.Scheduler.Features.Common;

[Serializable]
public sealed record PriceResponse(decimal Amount, string Currency);