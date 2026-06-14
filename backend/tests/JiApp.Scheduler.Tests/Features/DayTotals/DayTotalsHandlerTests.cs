using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.DayTotals;
using Microsoft.Data.Sqlite;

namespace JiApp.Scheduler.Tests.Features.DayTotals;

public sealed class DayTotalsHandlerTests
{
    private sealed class Fixture : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly SchedulerDbContext _db;
        private readonly Mock<ICurrentUserService> _currentUser;
        private readonly DateOnly _date;
        private readonly Board _board;
        private readonly Client _client;
        private readonly Service _service;

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
            _date = DateOnly.FromDateTime(DateTime.UtcNow);

            _board = new Board { Name = "Test Board", MemberUserIds = [1L] };
            _client = new Client { Name = "Alice", Board = _board };
            _service = new Service
            {
                Name = "Haircut",
                Category = ServiceCategory.MensHaircut,
                BaseDuration = 30,
                BasePrice = new Price(100)
            };

            _db.Boards.Add(_board);
            _db.Clients.Add(_client);
            _service.Board = _board;
            _db.Services.Add(_service);
            _db.SaveChanges();
            _db.ChangeTracker.Clear();
        }

        public SchedulerDbContext Db => _db;
        public ICurrentUserService CurrentUser => _currentUser.Object;
        public Board Board => _board;
        public Client Client => _client;
        public Service Service => _service;
        public DateOnly Date => _date;

        public Fixture WithAppointment(Action<Appointment>? configure = null)
        {
            var appointment = new Appointment
            {
                BoardId = _board.Id, ClientId = _client.Id, ServiceId = _service.Id,
                Date = _date, StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0),
                Price = new Price(200), Location = "Room 1",
                CreatedBy = 1L
            };
            configure?.Invoke(appointment);
            _db.Attach(_board);
            _db.Attach(_client);
            _db.Attach(_service);
            _db.Appointments.Add(appointment);
            _db.SaveChanges();
            _db.ChangeTracker.Clear();
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
            _db.Attach(_board);
            _db.Expenses.Add(expense);
            _db.SaveChanges();
            _db.ChangeTracker.Clear();
            return this;
        }

        public DayTotalsHandler Sut => new(_db, _currentUser.Object);

        public void Dispose()
        {
            _db.Dispose();
            _connection.Close();
        }
    }

    [Fact]
    public async Task DayTotals_WithRevenueAndExpenses_ReturnsCorrectNet()
    {
        using var fixture = new Fixture()
            .WithAppointment()
            .WithExpense("Fuel", 50)
            .WithExpense("Food", 30);
        var sut = fixture.Sut;

        var result = await sut.HandleAsync(new DayTotalsRequest(fixture.Board.Id, fixture.Date), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Revenue.Should().Be(200);
        result.Value!.Expenses.Should().Be(80);
        result.Value!.Net.Should().Be(120);
    }

    [Fact]
    public async Task DayTotals_WithCancelledAppointment_ExcludesFromRevenue()
    {
        using var fixture = new Fixture()
            .WithAppointment(a => { a.TryTransitionTo(AppointmentStatus.Cancelled, out _); });
        var sut = fixture.Sut;

        var result = await sut.HandleAsync(new DayTotalsRequest(fixture.Board.Id, fixture.Date), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Revenue.Should().Be(0);
        result.Value!.Expenses.Should().Be(0);
        result.Value!.Net.Should().Be(0);
    }

    [Fact]
    public async Task DayTotals_WithExpensesOnly_ReturnsNegativeNet()
    {
        using var fixture = new Fixture()
            .WithExpense("Supplies", 100);
        var sut = fixture.Sut;

        var result = await sut.HandleAsync(new DayTotalsRequest(fixture.Board.Id, fixture.Date), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Revenue.Should().Be(0);
        result.Value!.Expenses.Should().Be(100);
        result.Value!.Net.Should().Be(-100);
    }
}
