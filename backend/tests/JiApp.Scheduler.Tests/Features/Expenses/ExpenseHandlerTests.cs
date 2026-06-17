using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Features.Expenses.CreateExpense;
using JiApp.Scheduler.Features.Expenses.DeleteExpense;
using JiApp.Scheduler.Features.Expenses.GetExpense;
using JiApp.Scheduler.Features.Expenses.ListExpenses;
using JiApp.Scheduler.Features.Expenses.UpdateExpense;

namespace JiApp.Scheduler.Tests.Features.Expenses;

public sealed class ExpenseHandlerTests : HandlerTestBase
{
    private sealed class Fixture
    {
        private readonly ISchedulerDbContext _dbContext;
        private readonly TestDb _testDb;
        private readonly ICurrentUserService _currentUser;

        private Fixture(ISchedulerDbContext dbContext, TestDb testDb)
        {
            _dbContext = dbContext;
            _testDb = testDb;
            _currentUser = MockCurrentUserService.GetSuccessful().Mock.Object;
        }

        public CreateExpenseHandler Sut => new(_dbContext, _currentUser);
        public GetExpenseHandler GetExpense => new(_dbContext, _currentUser);
        public ListExpensesHandler ListExpenses => new(_dbContext, _currentUser);
        public UpdateExpenseHandler UpdateExpense => new(_dbContext, _currentUser);
        public DeleteExpenseHandler DeleteExpense => new(_dbContext, _currentUser);

        public static Fixture Init(ISchedulerDbContext dbContext, TestDb testDb) => new(dbContext, testDb);

        public Fixture WithBoard(string name = "Board", List<long>? memberUserIds = null)
        {
            var board = new Board { Name = name, MemberUserIds = memberUserIds ?? [1L] };
            _testDb.Store(board);
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
            _testDb.Store(expense);
            return this;
        }
    }

    [Fact]
    public async Task CreateExpense_WithValidData_ReturnsExpenseId()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard();
        var board = Db.Query<Board>().Single();
        var sut = fixture.Sut;
        var request = new CreateExpenseRequest(
            board.Id, new DateOnly(2026, 5, 30),
            "Fuel", new PriceRequest(50), "Gas station");

        var result = await sut.HandleAsync(request, CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateExpense_WithInvalidBoardId_ReturnsFailure()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.Sut;
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
        var fixture = Fixture.Init(DbContext, Db).WithBoard();
        var board = Db.Query<Board>().Single();
        var sut = fixture.Sut;
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
        var fixture = Fixture.Init(DbContext, Db).WithBoard();
        var board = Db.Query<Board>().Single();
        fixture.WithExpense(board, new DateOnly(2026, 5, 30), "Fuel", 75, "Tank");
        var expense = Db.Query<Expense>().Single();
        var sut = fixture.GetExpense;
        var result = await sut.HandleAsync(expense.Id, CancellationToken.None);

        AssertSuccess(result);
        result.Value!.Category.Should().Be("Fuel");
        result.Value.Amount.Should().Be(75);
        result.Value.Note.Should().Be("Tank");
    }

    [Fact]
    public async Task GetExpense_WithInvalidId_ReturnsFailure()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.GetExpense;
        var result = await sut.HandleAsync(999L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ListExpenses_ByBoardAndDate_ReturnsFiltered()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard();
        var board = Db.Query<Board>().Single();
        fixture.WithExpense(board, new DateOnly(2026, 5, 30), "Fuel", 50);
        fixture.WithExpense(board, new DateOnly(2026, 5, 31), "Food", 30);
        var sut = fixture.ListExpenses;
        var result = await sut.HandleAsync(board.Id, new DateOnly(2026, 5, 30), CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().ContainSingle(e => e.Category == "Fuel");
    }

    [Fact]
    public async Task ListExpenses_WithoutDate_ReturnsAllForBoard()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard();
        var board = Db.Query<Board>().Single();
        fixture.WithExpense(board, new DateOnly(2026, 5, 30), "Fuel", 50);
        fixture.WithExpense(board, new DateOnly(2026, 5, 31), "Food", 30);
        var sut = fixture.ListExpenses;
        var result = await sut.HandleAsync(board.Id, null, CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateExpense_WithValidData_Updates()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard();
        var board = Db.Query<Board>().Single();
        fixture.WithExpense(board, new DateOnly(2026, 5, 30), "Fuel", 50, "Old note");
        var expense = Db.Query<Expense>().Single();
        var sut = fixture.UpdateExpense;
        var request = new UpdateExpenseRequest(
            new DateOnly(2026, 5, 31), "Food", new PriceRequest(80), "Updated note");

        var result = await sut.HandleAsync(expense.Id, request, CancellationToken.None);

        AssertSuccess(result);
        var updated = Db.Find<Expense>(expense.Id);
        updated!.Category.Should().Be(ExpenseCategory.Food);
        updated.Amount.Amount.Should().Be(80);
        updated.Note.Should().Be("Updated note");
        updated.Date.Should().Be(new DateOnly(2026, 5, 31));
    }

    [Fact]
    public async Task UpdateExpense_WithInvalidId_ReturnsFailure()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.UpdateExpense;
        var request = new UpdateExpenseRequest(
            new DateOnly(2026, 5, 31), "Fuel", new PriceRequest(50), null);

        var result = await sut.HandleAsync(999L, request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Expense not found");
    }

    [Fact]
    public async Task DeleteExpense_WithValidId_Deletes()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard();
        var board = Db.Query<Board>().Single();
        fixture.WithExpense(board, new DateOnly(2026, 5, 30), "Fuel", 50);
        var expense = Db.Query<Expense>().Single();
        var sut = fixture.DeleteExpense;
        var result = await sut.HandleAsync(expense.Id, CancellationToken.None);

        AssertSuccess(result);
        var deleted = Db.Find<Expense>(expense.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteExpense_WithInvalidId_ReturnsFailure()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.DeleteExpense;
        var result = await sut.HandleAsync(999L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CreateExpense_WithInvalidCategory_ReturnsValidationErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard();
        var board = Db.Query<Board>().Single();
        var sut = fixture.Sut;
        var request = new CreateExpenseRequest(
            board.Id, new DateOnly(2026, 5, 30),
            "InvalidCategory", new PriceRequest(50), null);

        var result = await sut.HandleAsync(request, CancellationToken.None);

        AssertValidationFailure(result);
    }

    [Fact]
    public async Task UpdateExpense_WithInvalidCategory_ReturnsValidationErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard();
        var board = Db.Query<Board>().Single();
        fixture.WithExpense(board, new DateOnly(2026, 5, 30), "Fuel", 50);
        var expense = Db.Query<Expense>().Single();
        var sut = fixture.UpdateExpense;
        var request = new UpdateExpenseRequest(
            new DateOnly(2026, 5, 31), "InvalidCategory", new PriceRequest(80), null);

        var result = await sut.HandleAsync(expense.Id, request, CancellationToken.None);

        AssertValidationFailure(result);
    }

    [Fact]
    public async Task ListExpenses_WithInvalidBoard_ReturnsNotFoundErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.ListExpenses;
        var result = await sut.HandleAsync(999L, null, CancellationToken.None);

        AssertNotFound(result);
    }

    [Fact]
    public async Task ListExpenses_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(name: "Other", memberUserIds: [2L]);
        var otherBoard = Db.Query<Board>().Single();
        var sut = fixture.ListExpenses;
        var result = await sut.HandleAsync(otherBoard.Id, null, CancellationToken.None);

        AssertAccessDenied(result);
    }
}
