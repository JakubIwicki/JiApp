namespace JiApp.Scheduler.Tests.Domain;

public sealed class PriceTests
{
    [Fact]
    public void Price_DefaultCurrencyIsPLN()
    {
        var price = new Price();

        price.Amount.Should().Be(0m);
        price.Currency.Should().Be("PLN");
    }

    [Fact]
    public void Price_ConstructorSetsAmountAndCurrency()
    {
        var price = new Price(99.99m, "EUR");

        price.Amount.Should().Be(99.99m);
        price.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Price_ConstructorDefaultsToPLN()
    {
        var price = new Price(50m);

        price.Amount.Should().Be(50m);
        price.Currency.Should().Be("PLN");
    }
}