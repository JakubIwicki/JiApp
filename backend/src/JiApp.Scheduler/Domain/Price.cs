namespace JiApp.Scheduler.Domain;

public sealed record Price
{
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "PLN";

    public Price()
    {
    }

    public Price(decimal amount, string currency = "PLN")
    {
        Amount = amount;
        Currency = currency;
    }
}