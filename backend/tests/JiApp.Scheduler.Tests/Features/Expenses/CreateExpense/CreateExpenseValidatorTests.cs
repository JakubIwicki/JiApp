using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Features.Expenses.CreateExpense;

namespace JiApp.Scheduler.Tests.Features.Expenses.CreateExpense;

public sealed class CreateExpenseValidatorTests
{
    private sealed class Fixture
    {
        public CreateExpenseValidator Sut => new();

        public static Fixture Init() => new();
    }

    [Fact]
    public void Validate_WithZeroAmount_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new CreateExpenseRequest(
            1, new DateOnly(2026, 1, 3), "Fuel",
            new PriceRequest(0), null);

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithInvalidCategory_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new CreateExpenseRequest(
            1, new DateOnly(2026, 1, 3), "InvalidCategory",
            new PriceRequest(100), null);

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithValidRequest_IsValid()
    {
        var fixture = Fixture.Init();
        var request = new CreateExpenseRequest(
            1, new DateOnly(2026, 1, 3), "Fuel",
            new PriceRequest(100), null);

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
