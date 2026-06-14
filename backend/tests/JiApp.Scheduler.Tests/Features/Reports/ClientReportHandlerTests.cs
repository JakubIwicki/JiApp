using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Reports.ClientReport;
using Microsoft.Data.Sqlite;

namespace JiApp.Scheduler.Tests.Features.Reports;

public sealed class ClientReportHandlerTests
{
    private sealed class Fixture : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly SchedulerDbContext _db;
        private readonly Mock<ICurrentUserService> _currentUser;
        private readonly DateOnly _saturday;
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

        public SchedulerDbContext Db => _db;
        public ICurrentUserService CurrentUser => _currentUser.Object;
        public Board Board => _board;
        public Client Client => _client;
        public Service Service => _service;
        public DateOnly Saturday => _saturday;

        public Fixture WithAppointment(DateOnly date, decimal price, string location, long clientId, DateTime? createdAt = null)
        {
            var appointment = new Appointment
            {
                BoardId = _board.Id, ClientId = clientId, ServiceId = _service.Id,
                Date = date, StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0),
                Price = new Price(price), Location = location,
                CreatedBy = 1L
            };
            if (createdAt.HasValue)
                appointment.CreatedAt = createdAt.Value;
            _db.Attach(_board);
            _db.Attach(_client);
            _db.Attach(_service);
            _db.Appointments.Add(appointment);
            _db.SaveChanges();
            _db.ChangeTracker.Clear();
            return this;
        }

        public Fixture WithClient(string name)
        {
            var client = new Client { Name = name, Board = _board };
            _db.Attach(_board);
            _db.Clients.Add(client);
            _db.SaveChanges();
            _db.ChangeTracker.Clear();
            return this;
        }

        public ClientReportHandler Sut => new(_db, _currentUser.Object);

        public void Dispose()
        {
            _db.Dispose();
            _connection.Close();
        }
    }

    [Fact]
    public async Task ClientReport_SortByFrequency_ReturnsOrderedResults()
    {
        using var fixture = new Fixture();
        fixture.WithClient("Bob");
        var bob = fixture.Db.Clients.Single(c => c.Name == "Bob");

        // Alice has 2 visits, Bob has 1 visit
        fixture.WithAppointment(fixture.Saturday, 100, "Room 1", fixture.Client.Id);
        fixture.WithAppointment(fixture.Saturday.AddDays(7), 150, "Room 1", fixture.Client.Id);
        fixture.WithAppointment(fixture.Saturday, 80, "Room 2", bob.Id);
        var sut = fixture.Sut;

        var request = new ClientReportRequest(fixture.Board.Id, "frequency");
        var result = await sut.HandleAsync(request, CancellationToken.None);

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
        using var fixture = new Fixture();
        var earlyDate = new DateOnly(2026, 6, 1);
        var laterDate = new DateOnly(2026, 6, 15);
        fixture.WithAppointment(earlyDate, 100, "Room 1", fixture.Client.Id);
        fixture.WithAppointment(laterDate, 80, "Room 2", fixture.Client.Id);
        var sut = fixture.Sut;

        var request = new ClientReportRequest(fixture.Board.Id, "lastVisit");
        var result = await sut.HandleAsync(request, CancellationToken.None);

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
        using var fixture = new Fixture();
        fixture.WithAppointment(fixture.Saturday, 100, "Room 1", fixture.Client.Id,
            new DateTime(2026, 5, 30, 10, 0, 0, DateTimeKind.Utc));
        fixture.WithAppointment(fixture.Saturday.AddDays(7), 150, "Room 1", fixture.Client.Id,
            new DateTime(2026, 6, 6, 10, 0, 0, DateTimeKind.Utc));
        var sut = fixture.Sut;

        var request = new ClientReportRequest(fixture.Board.Id, "totalSpent");
        var result = await sut.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        var report = result.Value.Single();
        report.ClientName.Should().Be("Alice");
        report.VisitCount.Should().Be(2);
        report.TotalSpent.Should().Be(250);
        report.AveragePerVisit.Should().Be(125);
    }
}
