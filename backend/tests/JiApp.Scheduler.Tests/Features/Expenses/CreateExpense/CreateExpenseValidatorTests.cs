using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Features.Expenses.CreateExpense;

namespace JiApp.Scheduler.Tests.Features.Expenses.CreateExpense;

public sealed class CreateExpenseValidatorTests
{
    private readonly CreateExpenseValidator _sut = new();

    [Fact]
    public void Validate_WithZeroAmount_ReturnsError()
    {
        var request = new CreateExpenseRequest(
            1, new DateOnly(2026, 1, 3), "Fuel",
            new PriceRequest(0), null);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithInvalidCategory_ReturnsError()
    {
        var request = new CreateExpenseRequest(
            1, new DateOnly(2026, 1, 3), "InvalidCategory",
            new PriceRequest(100), null);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithValidRequest_IsValid()
    {
        var request = new CreateExpenseRequest(
            1, new DateOnly(2026, 1, 3), "Fuel",
            new PriceRequest(100), null);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}