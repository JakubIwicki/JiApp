namespace JiApp.Scheduler.Tests.Domain;

public sealed class ExpenseTests
{
    [Fact]
    public void Expense_HasDefaultValues()
    {
        var expense = new Expense();

        expense.Id.Should().Be(0L);
        expense.BoardId.Should().Be(0L);
        expense.Category.Should().Be(default(ExpenseCategory));
        expense.Note.Should().BeNull();
        expense.Amount.Should().NotBeNull();
        expense.Amount.Amount.Should().Be(0m);
        expense.Amount.Currency.Should().Be("PLN");
    }
}
