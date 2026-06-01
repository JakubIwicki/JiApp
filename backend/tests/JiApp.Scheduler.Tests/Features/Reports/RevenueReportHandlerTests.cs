using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Reports.RevenueReport;
using Microsoft.Data.Sqlite;

namespace JiApp.Scheduler.Tests.Features.Reports;

public sealed class RevenueReportHandlerTests : IDisposable
{
    private readonly Board _board;
    private readonly Client _client;
    private readonly SqliteConnection _connection;
    private readonly Mock<ICurrentUserService> _currentUser;
    private readonly SchedulerDbContext _db;
    private readonly DateOnly _saturday;
    private readonly Service _service;
    private readonly DateOnly _sunday;

    public RevenueReportHandlerTests()
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

        var start = DateOnly.FromDateTime(DateTime.UtcNow);
        while (start.DayOfWeek != DayOfWeek.Saturday)
            start = start.AddDays(1);
        _saturday = start;
        _sunday = _saturday.AddDays(1);

        _board = new Board { Name = "Board", MemberUserIds = [1L] };
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
    public async Task RevenueReport_InvalidGroupBy_ReturnsFailure()
    {
        var handler = new RevenueReportHandler(_db, _currentUser.Object);
        var request = new RevenueReportRequest(_board.Id, _saturday, _sunday, "invalid");
        var result = await handler.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid groupBy. Use: weekend, service, location, or client");
    }

    [Fact]
    public async Task RevenueReport_FromAfterTo_ReturnsFailure()
    {
        var handler = new RevenueReportHandler(_db, _currentUser.Object);
        var request = new RevenueReportRequest(_board.Id, _sunday, _saturday, "weekend");
        var result = await handler.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("From date must be before or equal to To date");
    }

    [Fact]
    public async Task RevenueReport_ByService_ReturnsGroupedData()
    {
        _db.Attach(_board);
        _db.Attach(_client);

        var service2 = new Service
        {
            Name = "Coloring", Category = ServiceCategory.Coloring, BaseDuration = 60,
            BasePrice = new Price(300), Board = _board
        };
        _db.Services.Add(service2);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        _db.Attach(_board);
        _db.Attach(_client);
        _db.Attach(_service);
        _db.Attach(service2);

        _db.Appointments.AddRange(
            new Appointment
            {
                BoardId = _board.Id, ClientId = _client.Id, ServiceId = _service.Id,
                Date = _saturday, StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0),
                Price = new Price(100), Location = "Room 1",
                 CreatedBy = 1L
            },
            new Appointment
            {
                BoardId = _board.Id, ClientId = _client.Id, ServiceId = service2.Id,
                Date = _saturday, StartTime = new TimeOnly(11, 0), EndTime = new TimeOnly(12, 0),
                Price = new Price(300), Location = "Room 2",
                 CreatedBy = 1L
            });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new RevenueReportHandler(_db, _currentUser.Object);
        var request = new RevenueReportRequest(_board.Id, _saturday, _saturday.AddDays(1), "service");
        var result = await handler.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        var haircut = result.Value.Single(r => r.GroupKey == "Haircut");
        haircut.Revenue.Should().Be(100);
        haircut.AppointmentCount.Should().Be(1);
    }

    [Fact]
    public async Task RevenueReport_ByLocation_ReturnsGroupedData()
    {
        _db.Attach(_board);
        _db.Attach(_client);
        _db.Attach(_service);

        _db.Appointments.AddRange(
            new Appointment
            {
                BoardId = _board.Id, ClientId = _client.Id, ServiceId = _service.Id,
                Date = _saturday, StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0),
                Price = new Price(100), Location = "Room 1",
                 CreatedBy = 1L
            },
            new Appointment
            {
                BoardId = _board.Id, ClientId = _client.Id, ServiceId = _service.Id,
                Date = _saturday, StartTime = new TimeOnly(11, 0), EndTime = new TimeOnly(12, 0),
                Price = new Price(150), Location = "Room 2",
                 CreatedBy = 1L
            });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new RevenueReportHandler(_db, _currentUser.Object);
        var request = new RevenueReportRequest(_board.Id, _saturday, _saturday.AddDays(1), "location");
        var result = await handler.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(r => r.GroupKey == "Room 1");
        result.Value.Should().Contain(r => r.GroupKey == "Room 2");
    }

    [Fact]
    public async Task RevenueReport_ByClient_ReturnsGroupedData()
    {
        _db.Attach(_board);
        _db.Attach(_client);
        _db.Attach(_service);

        _db.Appointments.Add(new Appointment
        {
            BoardId = _board.Id, ClientId = _client.Id, ServiceId = _service.Id,
            Date = _saturday, StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0),
            Price = new Price(100), Location = "Room 1",
             CreatedBy = 1L
        });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new RevenueReportHandler(_db, _currentUser.Object);
        var request = new RevenueReportRequest(_board.Id, _saturday, _saturday.AddDays(1), "client");
        var result = await handler.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(r => r.GroupKey == "Alice");
    }

    [Fact]
    public async Task RevenueReport_ByWeekend_GroupsSatAndSunTogether()
    {
        _db.Attach(_board);
        _db.Attach(_client);
        _db.Attach(_service);

        _db.Appointments.AddRange(
            new Appointment
            {
                BoardId = _board.Id, ClientId = _client.Id, ServiceId = _service.Id,
                Date = _saturday, StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0),
                Price = new Price(200), Location = "Room 1",
                 CreatedBy = 1L
            },
            new Appointment
            {
                BoardId = _board.Id, ClientId = _client.Id, ServiceId = _service.Id,
                Date = _sunday, StartTime = new TimeOnly(11, 0), EndTime = new TimeOnly(12, 0),
                Price = new Price(300), Location = "Room 2",
                 CreatedBy = 1L
            });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new RevenueReportHandler(_db, _currentUser.Object);
        var request = new RevenueReportRequest(_board.Id, _saturday, _sunday, "weekend");
        var result = await handler.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        var weekend = result.Value.Single();
        weekend.Revenue.Should().Be(500);
        weekend.AppointmentCount.Should().Be(2);
        weekend.Net.Should().Be(500);
    }

    [Fact]
    public async Task RevenueReport_InvalidGroupBy_ReturnsValidationErrorCategory()
    {
        var handler = new RevenueReportHandler(_db, _currentUser.Object);
        var request = new RevenueReportRequest(_board.Id, _saturday, _sunday, "invalid");

        var result = await handler.HandleAsync(request, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.Validation);
    }

    [Fact]
    public async Task RevenueReport_FromAfterTo_ReturnsValidationErrorCategory()
    {
        var handler = new RevenueReportHandler(_db, _currentUser.Object);
        var request = new RevenueReportRequest(_board.Id, _sunday, _saturday, "weekend");

        var result = await handler.HandleAsync(request, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.Validation);
    }

    [Fact]
    public async Task RevenueReport_WithInvalidBoard_ReturnsNotFoundErrorCategory()
    {
        var handler = new RevenueReportHandler(_db, _currentUser.Object);
        var request = new RevenueReportRequest(999L, _saturday, _sunday, "weekend");

        var result = await handler.HandleAsync(request, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }

    [Fact]
    public async Task RevenueReport_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        var otherBoard = new Board { Name = "Other", MemberUserIds = [2L] };
        _db.Boards.Add(otherBoard);
        await _db.SaveChangesAsync();

        var handler = new RevenueReportHandler(_db, _currentUser.Object);
        var request = new RevenueReportRequest(otherBoard.Id, _saturday, _sunday, "weekend");

        var result = await handler.HandleAsync(request, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.AccessDenied);
    }
}