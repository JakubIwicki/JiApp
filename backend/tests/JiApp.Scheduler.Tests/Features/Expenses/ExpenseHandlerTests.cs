using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Features.Expenses.CreateExpense;
using JiApp.Scheduler.Features.Expenses.DeleteExpense;
using JiApp.Scheduler.Features.Expenses.GetExpense;
using JiApp.Scheduler.Features.Expenses.ListExpenses;
using JiApp.Scheduler.Features.Expenses.UpdateExpense;
using Microsoft.Data.Sqlite;

namespace JiApp.Scheduler.Tests.Features.Expenses;

public sealed class ExpenseHandlerTests : IDisposable
{
    private readonly Board _board;
    private readonly SqliteConnection _connection;
    private readonly Mock<ICurrentUserService> _currentUser;
    private readonly SchedulerDbContext _db;

    public ExpenseHandlerTests()
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
    public async Task CreateExpense_WithValidData_ReturnsExpenseId()
    {
        var handler = new CreateExpenseHandler(_db, _currentUser.Object);
        var request = new CreateExpenseRequest(
            _board.Id, new DateOnly(2026, 5, 30),
            "Fuel", new PriceRequest(50), "Gas station");

        var result = await handler.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateExpense_WithInvalidBoardId_ReturnsFailure()
    {
        var handler = new CreateExpenseHandler(_db, _currentUser.Object);
        var request = new CreateExpenseRequest(
            999L, new DateOnly(2026, 5, 30),
            "Fuel", new PriceRequest(50), null);

        var result = await handler.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Board not found");
    }

    [Fact]
    public async Task CreateExpense_WithInvalidCategory_ReturnsFailure()
    {
        var handler = new CreateExpenseHandler(_db, _currentUser.Object);
        var request = new CreateExpenseRequest(
            _board.Id, new DateOnly(2026, 5, 30),
            "InvalidCategory", new PriceRequest(50), null);

        var result = await handler.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid expense category: InvalidCategory");
    }

    [Fact]
    public async Task GetExpense_WithValidId_ReturnsExpense()
    {
        _db.Attach(_board);
        var expense = new Expense
        {
            BoardId = _board.Id,
            Date = new DateOnly(2026, 5, 30),
            Category = ExpenseCategory.Fuel,
            Amount = new Price(75),
            Note = "Tank"
        };
        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new GetExpenseHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(expense.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Category.Should().Be("Fuel");
        result.Value.Amount.Should().Be(75);
        result.Value.Note.Should().Be("Tank");
    }

    [Fact]
    public async Task GetExpense_WithInvalidId_ReturnsFailure()
    {
        var handler = new GetExpenseHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(999L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ListExpenses_ByBoardAndDate_ReturnsFiltered()
    {
        _db.Attach(_board);
        _db.Expenses.AddRange(
            new Expense
            {
                BoardId = _board.Id, Date = new DateOnly(2026, 5, 30),
                Category = ExpenseCategory.Fuel, Amount = new Price(50)
            },
            new Expense
            {
                BoardId = _board.Id, Date = new DateOnly(2026, 5, 31),
                Category = ExpenseCategory.Food, Amount = new Price(30)
            });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new ListExpensesHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(_board.Id, new DateOnly(2026, 5, 30), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(e => e.Category == "Fuel");
    }

    [Fact]
    public async Task ListExpenses_WithoutDate_ReturnsAllForBoard()
    {
        _db.Attach(_board);
        _db.Expenses.AddRange(
            new Expense
            {
                BoardId = _board.Id, Date = new DateOnly(2026, 5, 30),
                Category = ExpenseCategory.Fuel, Amount = new Price(50)
            },
            new Expense
            {
                BoardId = _board.Id, Date = new DateOnly(2026, 5, 31),
                Category = ExpenseCategory.Food, Amount = new Price(30)
            });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new ListExpensesHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(_board.Id, null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateExpense_WithValidData_Updates()
    {
        _db.Attach(_board);
        var expense = new Expense
        {
            BoardId = _board.Id,
            Date = new DateOnly(2026, 5, 30),
            Category = ExpenseCategory.Fuel,
            Amount = new Price(50),
            Note = "Old note"
        };
        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new UpdateExpenseHandler(_db, _currentUser.Object);
        var request = new UpdateExpenseRequest(
            new DateOnly(2026, 5, 31), "Food", new PriceRequest(80), "Updated note");

        var result = await handler.HandleAsync(expense.Id, request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var updated = await _db.Expenses.FindAsync(expense.Id);
        updated!.Category.Should().Be(ExpenseCategory.Food);
        updated.Amount.Amount.Should().Be(80);
        updated.Note.Should().Be("Updated note");
        updated.Date.Should().Be(new DateOnly(2026, 5, 31));
    }

    [Fact]
    public async Task UpdateExpense_WithInvalidId_ReturnsFailure()
    {
        var handler = new UpdateExpenseHandler(_db, _currentUser.Object);
        var request = new UpdateExpenseRequest(
            new DateOnly(2026, 5, 31), "Fuel", new PriceRequest(50), null);

        var result = await handler.HandleAsync(999L, request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Expense not found");
    }

    [Fact]
    public async Task DeleteExpense_WithValidId_Deletes()
    {
        _db.Attach(_board);
        var expense = new Expense
        {
            BoardId = _board.Id,
            Date = new DateOnly(2026, 5, 30),
            Category = ExpenseCategory.Fuel,
            Amount = new Price(50)
        };
        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new DeleteExpenseHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(expense.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var deleted = await _db.Expenses.FindAsync(expense.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteExpense_WithInvalidId_ReturnsFailure()
    {
        var handler = new DeleteExpenseHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(999L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CreateExpense_WithInvalidCategory_ReturnsValidationErrorCategory()
    {
        var handler = new CreateExpenseHandler(_db, _currentUser.Object);
        var request = new CreateExpenseRequest(
            _board.Id, new DateOnly(2026, 5, 30),
            "InvalidCategory", new PriceRequest(50), null);

        var result = await handler.HandleAsync(request, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.Validation);
    }

    [Fact]
    public async Task UpdateExpense_WithInvalidCategory_ReturnsValidationErrorCategory()
    {
        _db.Attach(_board);
        var expense = new Expense
        {
            BoardId = _board.Id,
            Date = new DateOnly(2026, 5, 30),
            Category = ExpenseCategory.Fuel,
            Amount = new Price(50)
        };
        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new UpdateExpenseHandler(_db, _currentUser.Object);
        var request = new UpdateExpenseRequest(
            new DateOnly(2026, 5, 31), "InvalidCategory", new PriceRequest(80), null);

        var result = await handler.HandleAsync(expense.Id, request, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.Validation);
    }

    [Fact]
    public async Task ListExpenses_WithInvalidBoard_ReturnsNotFoundErrorCategory()
    {
        var handler = new ListExpensesHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(999L, null, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }

    [Fact]
    public async Task ListExpenses_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        var otherBoard = new Board { Name = "Other", MemberUserIds = [2L] };
        _db.Boards.Add(otherBoard);
        await _db.SaveChangesAsync();

        var handler = new ListExpensesHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(otherBoard.Id, null, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.AccessDenied);
    }
}