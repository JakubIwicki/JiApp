using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Features.Expenses.UpdateExpense;

namespace JiApp.Scheduler.Tests.Features.Expenses.UpdateExpense;

public sealed class UpdateExpenseValidatorTests
{
    private readonly UpdateExpenseValidator _sut = new();

    [Fact]
    public void Validate_WithZeroAmount_ReturnsError()
    {
        var request = new UpdateExpenseRequest(
            new DateOnly(2026, 1, 3), "Fuel",
            new PriceRequest(0), null);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithInvalidCategory_ReturnsError()
    {
        var request = new UpdateExpenseRequest(
            new DateOnly(2026, 1, 3), "InvalidCategory",
            new PriceRequest(100), null);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithValidRequest_IsValid()
    {
        var request = new UpdateExpenseRequest(
            new DateOnly(2026, 1, 3), "Fuel",
            new PriceRequest(100), null);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}