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

public sealed class ExpenseHandlerTests
{
    private sealed class Fixture : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly SchedulerDbContext _db;
        private readonly Mock<ICurrentUserService> _currentUser;

        public Fixture()
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
        }

        public SchedulerDbContext Db => _db;
        public ICurrentUserService CurrentUser => _currentUser.Object;

        public Fixture WithBoard(string name = "Board", List<long>? memberUserIds = null)
        {
            var board = new Board { Name = name, MemberUserIds = memberUserIds ?? [1L] };
            _db.Boards.Add(board);
            _db.SaveChanges();
            _db.ChangeTracker.Clear();
            return this;
        }

        public Fixture WithExpense(Board board, DateOnly date, string category, decimal amount, string? note = null)
        {
            var expense = new Expense
            {
                BoardId = board.Id,
                Date = date,
                Category = Enum.Parse<ExpenseCategory>(category),
                Amount = new Price(amount),
                Note = note
            };
            _db.Attach(board);
            _db.Expenses.Add(expense);
            _db.SaveChanges();
            _db.ChangeTracker.Clear();
            return this;
        }

        public CreateExpenseHandler CreateExpenseSut => new(_db, _currentUser.Object);
        public GetExpenseHandler GetExpenseSut => new(_db, _currentUser.Object);
        public ListExpensesHandler ListExpensesSut => new(_db, _currentUser.Object);
        public UpdateExpenseHandler UpdateExpenseSut => new(_db, _currentUser.Object);
        public DeleteExpenseHandler DeleteExpenseSut => new(_db, _currentUser.Object);

        public void Dispose()
        {
            _db.Dispose();
            _connection.Close();
        }
    }

    [Fact]
    public async Task CreateExpense_WithValidData_ReturnsExpenseId()
    {
        using var fixture = new Fixture().WithBoard();
        var board = fixture.Db.Boards.Single();
        var sut = fixture.CreateExpenseSut;
        var request = new CreateExpenseRequest(
            board.Id, new DateOnly(2026, 5, 30),
            "Fuel", new PriceRequest(50), "Gas station");

        var result = await sut.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateExpense_WithInvalidBoardId_ReturnsFailure()
    {
        using var fixture = new Fixture();
        var sut = fixture.CreateExpenseSut;
        var request = new CreateExpenseRequest(
            999L, new DateOnly(2026, 5, 30),
            "Fuel", new PriceRequest(50), null);

        var result = await sut.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Board not found");
    }

    [Fact]
    public async Task CreateExpense_WithInvalidCategory_ReturnsFailure()
    {
        using var fixture = new Fixture().WithBoard();
        var board = fixture.Db.Boards.Single();
        var sut = fixture.CreateExpenseSut;
        var request = new CreateExpenseRequest(
            board.Id, new DateOnly(2026, 5, 30),
            "InvalidCategory", new PriceRequest(50), null);

        var result = await sut.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid expense category: InvalidCategory");
    }

    [Fact]
    public async Task GetExpense_WithValidId_ReturnsExpense()
    {
        using var fixture = new Fixture().WithBoard();
        var board = fixture.Db.Boards.Single();
        fixture.WithExpense(board, new DateOnly(2026, 5, 30), "Fuel", 75, "Tank");
        var expense = fixture.Db.Expenses.Single();
        var sut = fixture.GetExpenseSut;
        var result = await sut.HandleAsync(expense.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Category.Should().Be("Fuel");
        result.Value.Amount.Should().Be(75);
        result.Value.Note.Should().Be("Tank");
    }

    [Fact]
    public async Task GetExpense_WithInvalidId_ReturnsFailure()
    {
        using var fixture = new Fixture();
        var sut = fixture.GetExpenseSut;
        var result = await sut.HandleAsync(999L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ListExpenses_ByBoardAndDate_ReturnsFiltered()
    {
        using var fixture = new Fixture().WithBoard();
        var board = fixture.Db.Boards.Single();
        fixture.WithExpense(board, new DateOnly(2026, 5, 30), "Fuel", 50);
        fixture.WithExpense(board, new DateOnly(2026, 5, 31), "Food", 30);
        var sut = fixture.ListExpensesSut;
        var result = await sut.HandleAsync(board.Id, new DateOnly(2026, 5, 30), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(e => e.Category == "Fuel");
    }

    [Fact]
    public async Task ListExpenses_WithoutDate_ReturnsAllForBoard()
    {
        using var fixture = new Fixture().WithBoard();
        var board = fixture.Db.Boards.Single();
        fixture.WithExpense(board, new DateOnly(2026, 5, 30), "Fuel", 50);
        fixture.WithExpense(board, new DateOnly(2026, 5, 31), "Food", 30);
        var sut = fixture.ListExpensesSut;
        var result = await sut.HandleAsync(board.Id, null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateExpense_WithValidData_Updates()
    {
        using var fixture = new Fixture().WithBoard();
        var board = fixture.Db.Boards.Single();
        fixture.WithExpense(board, new DateOnly(2026, 5, 30), "Fuel", 50, "Old note");
        var expense = fixture.Db.Expenses.Single();
        var sut = fixture.UpdateExpenseSut;
        var request = new UpdateExpenseRequest(
            new DateOnly(2026, 5, 31), "Food", new PriceRequest(80), "Updated note");

        var result = await sut.HandleAsync(expense.Id, request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var updated = await fixture.Db.Expenses.FindAsync(expense.Id);
        updated!.Category.Should().Be(ExpenseCategory.Food);
        updated.Amount.Amount.Should().Be(80);
        updated.Note.Should().Be("Updated note");
        updated.Date.Should().Be(new DateOnly(2026, 5, 31));
    }

    [Fact]
    public async Task UpdateExpense_WithInvalidId_ReturnsFailure()
    {
        using var fixture = new Fixture();
        var sut = fixture.UpdateExpenseSut;
        var request = new UpdateExpenseRequest(
            new DateOnly(2026, 5, 31), "Fuel", new PriceRequest(50), null);

        var result = await sut.HandleAsync(999L, request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Expense not found");
    }

    [Fact]
    public async Task DeleteExpense_WithValidId_Deletes()
    {
        using var fixture = new Fixture().WithBoard();
        var board = fixture.Db.Boards.Single();
        fixture.WithExpense(board, new DateOnly(2026, 5, 30), "Fuel", 50);
        var expense = fixture.Db.Expenses.Single();
        var sut = fixture.DeleteExpenseSut;
        var result = await sut.HandleAsync(expense.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var deleted = await fixture.Db.Expenses.FindAsync(expense.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteExpense_WithInvalidId_ReturnsFailure()
    {
        using var fixture = new Fixture();
        var sut = fixture.DeleteExpenseSut;
        var result = await sut.HandleAsync(999L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CreateExpense_WithInvalidCategory_ReturnsValidationErrorCategory()
    {
        using var fixture = new Fixture().WithBoard();
        var board = fixture.Db.Boards.Single();
        var sut = fixture.CreateExpenseSut;
        var request = new CreateExpenseRequest(
            board.Id, new DateOnly(2026, 5, 30),
            "InvalidCategory", new PriceRequest(50), null);

        var result = await sut.HandleAsync(request, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.Validation);
    }

    [Fact]
    public async Task UpdateExpense_WithInvalidCategory_ReturnsValidationErrorCategory()
    {
        using var fixture = new Fixture().WithBoard();
        var board = fixture.Db.Boards.Single();
        fixture.WithExpense(board, new DateOnly(2026, 5, 30), "Fuel", 50);
        var expense = fixture.Db.Expenses.Single();
        var sut = fixture.UpdateExpenseSut;
        var request = new UpdateExpenseRequest(
            new DateOnly(2026, 5, 31), "InvalidCategory", new PriceRequest(80), null);

        var result = await sut.HandleAsync(expense.Id, request, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.Validation);
    }

    [Fact]
    public async Task ListExpenses_WithInvalidBoard_ReturnsNotFoundErrorCategory()
    {
        using var fixture = new Fixture();
        var sut = fixture.ListExpensesSut;
        var result = await sut.HandleAsync(999L, null, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }

    [Fact]
    public async Task ListExpenses_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        using var fixture = new Fixture().WithBoard(name: "Other", memberUserIds: [2L]);
        var otherBoard = fixture.Db.Boards.Single();
        var sut = fixture.ListExpensesSut;
        var result = await sut.HandleAsync(otherBoard.Id, null, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.AccessDenied);
    }
}
