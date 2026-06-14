using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Features.Expenses.UpdateExpense;
using Microsoft.Data.Sqlite;

namespace JiApp.Scheduler.Tests.Features.Expenses;

public sealed class UpdateExpenseHandlerXssTests
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

        public UpdateExpenseHandler Sut => new(_db, _currentUser.Object);

        public void Dispose()
        {
            _db.Dispose();
            _connection.Close();
        }
    }

    [Fact]
    public async Task HandleAsync_WithXssCategory_ErrorMessageDoesNotReflectInput()
    {
        using var fixture = new Fixture().WithBoard();
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
