namespace JiApp.Scheduler.Tests.Domain;

public sealed class ServiceTests
{
    [Fact]
    public void Service_HasDefaultValues()
    {
        var service = new Service();

        service.Id.Should().Be(0L);
        service.BoardId.Should().Be(0L);
        service.Name.Should().BeEmpty();
        service.Category.Should().Be(default(ServiceCategory));
        service.BaseDuration.Should().Be(0);
        service.BasePrice.Should().NotBeNull();
        service.BasePrice.Amount.Should().Be(0m);
        service.BasePrice.Currency.Should().Be("PLN");
    }
}
