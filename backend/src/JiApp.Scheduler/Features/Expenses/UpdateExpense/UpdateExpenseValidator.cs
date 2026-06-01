using FluentValidation;
using JiApp.Scheduler.Domain;

namespace JiApp.Scheduler.Features.Expenses.UpdateExpense;

public sealed class UpdateExpenseValidator : AbstractValidator<UpdateExpenseRequest>
{
    private static readonly string[] AllowedCurrencies = ["PLN", "EUR", "USD", "GBP", "CZK", "CHF"];

    public UpdateExpenseValidator()
    {
        RuleFor(x => x.Amount.Amount).GreaterThan(0).WithMessage("Amount must be greater than 0");
        RuleFor(x => x.Amount.Currency)
            .MaximumLength(3)
            .Must(c => AllowedCurrencies.Contains(c))
            .WithMessage($"Currency must be one of: {string.Join(", ", AllowedCurrencies)}");
        RuleFor(x => x.Category)
            .Must(c => Enum.TryParse<ExpenseCategory>(c, true, out _))
            .WithMessage("Invalid expense category. Must be one of: Fuel, Hotel, Parking, Supplies, Food, Other");
        RuleFor(x => x.Note).MaximumLength(1000).When(x => x.Note is not null);
    }
}