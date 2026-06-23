using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.DayTotals;

namespace JiApp.Scheduler.Tests.Features.DayTotals;

public sealed class DayTotalsHandlerTests : HandlerTestBase
{
    private sealed class Fixture
    {
        private readonly ISchedulerDbContext _dbContext;
        private readonly SchedulerDbContext _db;
        private readonly TestDb _testDb;
        private readonly ICurrentUserService _currentUser;
        private readonly DateOnly _date;
        private readonly Board _board;

        private Fixture(ISchedulerDbContext dbContext, TestDb testDb)
        {
            _dbContext = dbContext;
            _db = (SchedulerDbContext)dbContext;
            _testDb = testDb;
            _currentUser = MockCurrentUserService.GetSuccessful().Mock.Object;
            _date = DateOnly.FromDateTime(DateTime.UtcNow);

            _board = new Board { Name = "Test Board", MemberUserIds = [1L] };
            var client = new Client { Name = "Alice", Board = _board };
            var service = new Service
            {
                Name = "Haircut",
                Category = ServiceCategory.MensHaircut,
                BaseDuration = 30,
                BasePrice = new Price(100),
                Board = _board
            };

            _db.Boards.Add(_board);
            _db.Clients.Add(client);
            _db.Services.Add(service);
            _db.SaveChanges();
            _db.ChangeTracker.Clear();

            Client = client;
            Service = service;
        }

        public Board Board => _board;
        public Client Client { get; }
        public Service Service { get; }
        public DateOnly Date => _date;

        public DayTotalsHandler Sut => new(_dbContext, _currentUser);

        public static Fixture Init(ISchedulerDbContext dbContext, TestDb testDb) => new(dbContext, testDb);

        public Fixture WithAppointment(Action<Appointment>? configure = null)
        {
            var appointment = new Appointment
            {
                BoardId = _board.Id, ClientId = Client.Id, ServiceId = Service.Id,
                Date = _date, StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0),
                Price = new Price(200), Location = "Room 1",
                CreatedBy = 1L
            };
            configure?.Invoke(appointment);
            _testDb.Store(appointment);
            return this;
        }

        public Fixture WithExpense(string category, decimal amount)
        {
            var expense = new Expense
            {
                BoardId = _board.Id, Date = _date,
                Category = Enum.Parse<ExpenseCategory>(category),
                Amount = new Price(amount)
            };
            _testDb.Store(expense);
            return this;
        }
    }

    [Fact]
    public async Task DayTotals_WithRevenueAndExpenses_ReturnsCorrectNet()
    {
        var fixture = Fixture.Init(DbContext, Db)
            .WithAppointment()
            .WithExpense("Fuel", 50)
            .WithExpense("Food", 30);
        var sut = fixture.Sut;

        var result = await sut.HandleAsync(new DayTotalsRequest(fixture.Board.Id, fixture.Date), CancellationToken.None);

        AssertSuccess(result);
        result.Value!.Revenue.Should().Be(200);
        result.Value!.Expenses.Should().Be(80);
        result.Value!.Net.Should().Be(120);
    }

    [Fact]
    public async Task DayTotals_WithCancelledAppointment_ExcludesFromRevenue()
    {
        var fixture = Fixture.Init(DbContext, Db)
            .WithAppointment(a => { a.TryTransitionTo(AppointmentStatus.Cancelled, out _); });
        var sut = fixture.Sut;

        var result = await sut.HandleAsync(new DayTotalsRequest(fixture.Board.Id, fixture.Date), CancellationToken.None);

        AssertSuccess(result);
        result.Value!.Revenue.Should().Be(0);
        result.Value!.Expenses.Should().Be(0);
        result.Value!.Net.Should().Be(0);
    }

    [Fact]
    public async Task DayTotals_WithExpensesOnly_ReturnsNegativeNet()
    {
        var fixture = Fixture.Init(DbContext, Db)
            .WithExpense("Supplies", 100);
        var sut = fixture.Sut;

        var result = await sut.HandleAsync(new DayTotalsRequest(fixture.Board.Id, fixture.Date), CancellationToken.None);

        AssertSuccess(result);
        result.Value!.Revenue.Should().Be(0);
        result.Value!.Expenses.Should().Be(100);
        result.Value!.Net.Should().Be(-100);
    }
}
