using JiApp.Common.Models;

namespace JiApp.Scheduler.Domain;

public sealed class Expense : BaseEntity<long>
{
    public long BoardId { get; set; }
    public DateOnly Date { get; set; }
    public ExpenseCategory Category { get; set; }
    public Price Amount { get; set; } = new();
    public string? Note { get; set; }
    public Board Board { get; set; } = null!;
}