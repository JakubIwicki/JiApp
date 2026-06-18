using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Features.Expenses.UpdateExpense;

namespace JiApp.Scheduler.Tests.Features.Expenses;

public sealed class UpdateExpenseHandlerXssTests : HandlerTestBase
{
    private sealed class Fixture
    {
        private readonly ISchedulerDbContext _dbContext;
        private readonly SchedulerDbContext _db;
        private readonly ICurrentUserService _currentUser;

        private Fixture(ISchedulerDbContext dbContext, TestDb testDb)
        {
            _dbContext = dbContext;
            _db = (SchedulerDbContext)dbContext;
            _currentUser = MockCurrentUserService.GetSuccessful().Mock.Object;
        }

        public SchedulerDbContext Db => _db;
        public UpdateExpenseHandler Sut => new(_dbContext, _currentUser);

        public static Fixture Init(ISchedulerDbContext dbContext, TestDb testDb) => new(dbContext, testDb);

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
            _db.Expenses.Add(expense);
            _db.SaveChanges();
            _db.ChangeTracker.Clear();
            return this;
        }
    }

    [Fact]
    public async Task HandleAsync_WithXssCategory_ErrorMessageDoesNotReflectInput()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard();
        var board = fixture.Db.Boards.Single();
        fixture.WithExpense(board, DateOnly.FromDateTime(DateTime.UtcNow), "Fuel", 50);
        var expense = fixture.Db.Expenses.Single();
        var sut = fixture.Sut;

        var request = new UpdateExpenseRequest(
            DateOnly.FromDateTime(DateTime.UtcNow),
            "<script>alert('xss')</script>",
            new PriceRequest(100),
            null);

        var result = await sut.HandleAsync(expense.Id, request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotContain("<script>");
        result.Error.Should().NotContain(request.Category);
    }
}
