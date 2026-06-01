namespace JiApp.Scheduler.Features.Common;

[Serializable]
public sealed record PriceRequest(decimal Amount, string Currency = "PLN");