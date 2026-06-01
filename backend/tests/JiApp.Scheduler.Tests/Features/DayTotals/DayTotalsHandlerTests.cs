using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.DayTotals;
using Microsoft.Data.Sqlite;

namespace JiApp.Scheduler.Tests.Features.DayTotals;

public sealed class DayTotalsHandlerTests : IDisposable
{
    private readonly Board _board;
    private readonly Client _client;
    private readonly SqliteConnection _connection;
    private readonly Mock<ICurrentUserService> _currentUser;
    private readonly DateOnly _date;
    private readonly SchedulerDbContext _db;
    private readonly Service _service;

    public DayTotalsHandlerTests()
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

    public void Dispose()
    {
        _db.Dispose();
        _connection.Close();
    }

    [Fact]
    public async Task DayTotals_WithRevenueAndExpenses_ReturnsCorrectNet()
    {
        _db.Attach(_board);
        _db.Attach(_client);
        _db.Attach(_service);

        var appointment = new Appointment
        {
            BoardId = _board.Id, ClientId = _client.Id, ServiceId = _service.Id,
            Date = _date, StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0),
            Price = new Price(200), Location = "Room 1",
            CreatedBy = 1L
        };
        _db.Appointments.Add(appointment);

        _db.Expenses.AddRange(
            new Expense
            {
                BoardId = _board.Id, Date = _date, Category = ExpenseCategory.Fuel,
                Amount = new Price(50)
            },
            new Expense
            {
                BoardId = _board.Id, Date = _date, Category = ExpenseCategory.Food,
                Amount = new Price(30)
            });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new DayTotalsHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(new DayTotalsRequest(_board.Id, _date), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Revenue.Should().Be(200);
        result.Value!.Expenses.Should().Be(80);
        result.Value!.Net.Should().Be(120);
    }

    [Fact]
    public async Task DayTotals_WithCancelledAppointment_ExcludesFromRevenue()
    {
        _db.Attach(_board);
        _db.Attach(_client);
        _db.Attach(_service);

        var cancelledAppointment = new Appointment
        {
            BoardId = _board.Id, ClientId = _client.Id, ServiceId = _service.Id,
            Date = _date, StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0),
            Price = new Price(200), Location = "Room 1",
            CreatedBy = 1L
        };
        cancelledAppointment.TryTransitionTo(AppointmentStatus.Cancelled, out _);
        _db.Appointments.Add(cancelledAppointment);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new DayTotalsHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(new DayTotalsRequest(_board.Id, _date), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Revenue.Should().Be(0);
        result.Value!.Expenses.Should().Be(0);
        result.Value!.Net.Should().Be(0);
    }

    [Fact]
    public async Task DayTotals_WithExpensesOnly_ReturnsNegativeNet()
    {
        _db.Attach(_board);

        _db.Expenses.Add(new Expense
        {
            BoardId = _board.Id, Date = _date, Category = ExpenseCategory.Supplies,
            Amount = new Price(100)
        });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new DayTotalsHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(new DayTotalsRequest(_board.Id, _date), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Revenue.Should().Be(0);
        result.Value!.Expenses.Should().Be(100);
        result.Value!.Net.Should().Be(-100);
    }
}