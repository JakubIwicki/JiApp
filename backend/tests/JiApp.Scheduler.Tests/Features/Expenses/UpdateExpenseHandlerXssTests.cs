using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Features.Expenses.UpdateExpense;
using Microsoft.Data.Sqlite;

namespace JiApp.Scheduler.Tests.Features.Expenses;

public sealed class UpdateExpenseHandlerXssTests : IDisposable
{
    private readonly Board _board;
    private readonly SqliteConnection _connection;
    private readonly Mock<ICurrentUserService> _currentUser;
    private readonly SchedulerDbContext _db;

    public UpdateExpenseHandlerXssTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SchedulerDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new SchedulerDbContext(options);
        _db.Database.EnsureCreated();

        _currentUser = new Mock<ICurrentUserService>();
        _currentUser.Setup(x => x.UserId).Returns(1L);

        _board = new Board { Name = "Board", MemberUserIds = [1L] };
        _db.Boards.Add(_board);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Close();
    }

    [Fact]
    public async Task HandleAsync_WithXssCategory_ErrorMessageDoesNotReflectInput()
    {
        _db.Attach(_board);
        var expense = new Expense
        {
            BoardId = _board.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Category = ExpenseCategory.Fuel,
            Amount = new Price(50),
            Note = null
        };
        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        _db.Attach(_board);

        var handler = new UpdateExpenseHandler(_db, _currentUser.Object);
        var request = new UpdateExpenseRequest(
            DateOnly.FromDateTime(DateTime.UtcNow),
            "<script>alert('xss')</script>",
            new PriceRequest(100),
            null);

        var result = await handler.HandleAsync(expense.Id, request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotContain("<script>");
        result.Error.Should().NotContain(request.Category);
    }
}
