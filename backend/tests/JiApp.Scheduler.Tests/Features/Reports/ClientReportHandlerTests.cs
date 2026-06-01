using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Reports.ClientReport;
using Microsoft.Data.Sqlite;

namespace JiApp.Scheduler.Tests.Features.Reports;

public sealed class ClientReportHandlerTests : IDisposable
{
    private readonly Board _board;
    private readonly Client _client;
    private readonly SqliteConnection _connection;
    private readonly Mock<ICurrentUserService> _currentUser;
    private readonly SchedulerDbContext _db;
    private readonly DateOnly _saturday;
    private readonly Service _service;

    public ClientReportHandlerTests()
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
    public async Task ClientReport_SortByFrequency_ReturnsOrderedResults()
    {
        _db.Attach(_board);
        _db.Attach(_client);
        _db.Attach(_service);

        var client2 = new Client { Name = "Bob", Board = _board };
        _db.Clients.Add(client2);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        _db.Attach(_board);
        _db.Attach(_client);
        _db.Attach(_service);
        _db.Attach(client2);

        // Alice has 2 visits, Bob has 1 visit
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
                Date = _saturday.AddDays(7), StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0),
                Price = new Price(150), Location = "Room 1",
                 CreatedBy = 1L
            },
            new Appointment
            {
                BoardId = _board.Id, ClientId = client2.Id, ServiceId = _service.Id,
                Date = _saturday, StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(15, 0),
                Price = new Price(80), Location = "Room 2",
                 CreatedBy = 1L
            });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new ClientReportHandler(_db, _currentUser.Object);
        var request = new ClientReportRequest(_board.Id, "frequency");
        var result = await handler.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].ClientName.Should().Be("Alice");
        result.Value[0].VisitCount.Should().Be(2);
        result.Value[1].ClientName.Should().Be("Bob");
        result.Value[1].VisitCount.Should().Be(1);
    }

    [Fact]
    public async Task ClientReport_SortByLastVisit_ReturnsCorrectLastVisitDate()
    {
        _db.Attach(_board);
        _db.Attach(_client);
        _db.Attach(_service);

        var earlyDate = new DateOnly(2026, 6, 1);
        var laterDate = new DateOnly(2026, 6, 15);
        _db.Appointments.AddRange(
            new Appointment
            {
                BoardId = _board.Id, ClientId = _client.Id, ServiceId = _service.Id,
                Date = earlyDate, StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0),
                Price = new Price(100), Location = "Room 1",
                 CreatedBy = 1L
            },
            new Appointment
            {
                BoardId = _board.Id, ClientId = _client.Id, ServiceId = _service.Id,
                Date = laterDate, StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(15, 0),
                Price = new Price(80), Location = "Room 2",
                 CreatedBy = 1L
            });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new ClientReportHandler(_db, _currentUser.Object);
        var request = new ClientReportRequest(_board.Id, "lastVisit");
        var result = await handler.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        var report = result.Value.Single();
        report.VisitCount.Should().Be(2);
        report.TotalSpent.Should().Be(180);
        report.LastVisitDate.Should().Be(laterDate);
        report.AveragePerVisit.Should().Be(90);
    }

    [Fact]
    public async Task ClientReport_ReturnsClientAggregates()
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
                 CreatedBy = 1L,
                CreatedAt = new DateTime(2026, 5, 30, 10, 0, 0, DateTimeKind.Utc)
            },
            new Appointment
            {
                BoardId = _board.Id, ClientId = _client.Id, ServiceId = _service.Id,
                Date = _saturday.AddDays(7), StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0),
                Price = new Price(150), Location = "Room 1",
                 CreatedBy = 1L,
                CreatedAt = new DateTime(2026, 6, 6, 10, 0, 0, DateTimeKind.Utc)
            });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new ClientReportHandler(_db, _currentUser.Object);
        var request = new ClientReportRequest(_board.Id, "totalSpent");
        var result = await handler.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        var report = result.Value.Single();
        report.ClientName.Should().Be("Alice");
        report.VisitCount.Should().Be(2);
        report.TotalSpent.Should().Be(250);
        report.AveragePerVisit.Should().Be(125);
    }
}