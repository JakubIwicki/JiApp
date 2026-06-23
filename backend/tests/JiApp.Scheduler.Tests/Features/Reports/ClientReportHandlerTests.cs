using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Reports.ClientReport;

namespace JiApp.Scheduler.Tests.Features.Reports;

public sealed class ClientReportHandlerTests : HandlerTestBase
{
    private sealed class Fixture
    {
        private readonly ISchedulerDbContext _dbContext;
        private readonly SchedulerDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly DateOnly _saturday;
        private Board? _board;
        private Client? _client;
        private Service? _service;

        private Fixture(ISchedulerDbContext dbContext, TestDb testDb)
        {
            _dbContext = dbContext;
            _db = (SchedulerDbContext)dbContext;
            _currentUser = MockCurrentUserService.GetSuccessful().Mock.Object;

            var start = DateOnly.FromDateTime(DateTime.UtcNow);
            while (start.DayOfWeek != DayOfWeek.Saturday)
                start = start.AddDays(1);
            _saturday = start;
        }

        public SchedulerDbContext Db => _db;
        public Board Board => _board!;
        public Client Client => _client!;
        public Service Service => _service!;
        public DateOnly Saturday => _saturday;

        public ClientReportHandler Sut => new(_dbContext, _currentUser);

        public static Fixture Init(ISchedulerDbContext dbContext, TestDb testDb) => new(dbContext, testDb);

        public Fixture WithSeededEntities()
        {
            var board = new Board { Name = "Board", MemberUserIds = [1L] };
            var client = new Client { Name = "Alice", Board = board };
            var service = new Service
            {
                Name = "Haircut",
                Category = ServiceCategory.MensHaircut,
                BaseDuration = 30,
                BasePrice = new Price(100),
                Board = board
            };

            _db.Boards.Add(board);
            _db.Clients.Add(client);
            _db.Services.Add(service);
            _db.SaveChanges();
            _db.ChangeTracker.Clear();

            _board = board;
            _client = client;
            _service = service;

            return this;
        }

        public Fixture WithAppointment(DateOnly date, decimal price, string location, long clientId, DateTime? createdAt = null)
        {
            var appointment = new Appointment
            {
                BoardId = _board!.Id, ClientId = clientId, ServiceId = _service!.Id,
                Date = date, StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0),
                Price = new Price(price), Location = location,
                CreatedBy = 1L
            };
            if (createdAt.HasValue)
                appointment.CreatedAt = createdAt.Value;
            _db.Appointments.Add(appointment);
            _db.SaveChanges();
            _db.ChangeTracker.Clear();
            return this;
        }

        public Fixture WithClient(string name)
        {
            var client = new Client { Name = name, BoardId = _board!.Id };
            _db.Clients.Add(client);
            _db.SaveChanges();
            _db.ChangeTracker.Clear();
            return this;
        }
    }

    [Fact]
    public async Task ClientReport_SortByFrequency_ReturnsOrderedResults()
    {
        var fixture = Fixture.Init(DbContext, Db).WithSeededEntities();
        fixture.WithClient("Bob");
        var bob = fixture.Db.Clients.Single(c => c.Name == "Bob");

        fixture.WithAppointment(fixture.Saturday, 100, "Room 1", fixture.Client.Id);
        fixture.WithAppointment(fixture.Saturday.AddDays(7), 150, "Room 1", fixture.Client.Id);
        fixture.WithAppointment(fixture.Saturday, 80, "Room 2", bob.Id);
        var sut = fixture.Sut;

        var request = new ClientReportRequest(fixture.Board.Id, "frequency");
        var result = await sut.HandleAsync(request, CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().HaveCount(2);
        result.Value[0].ClientName.Should().Be("Alice");
        result.Value[0].VisitCount.Should().Be(2);
        result.Value[1].ClientName.Should().Be("Bob");
        result.Value[1].VisitCount.Should().Be(1);
    }

    [Fact]
    public async Task ClientReport_SortByLastVisit_ReturnsCorrectLastVisitDate()
    {
        var fixture = Fixture.Init(DbContext, Db).WithSeededEntities();
        var earlyDate = new DateOnly(2026, 6, 1);
        var laterDate = new DateOnly(2026, 6, 15);
        fixture.WithAppointment(earlyDate, 100, "Room 1", fixture.Client.Id);
        fixture.WithAppointment(laterDate, 80, "Room 2", fixture.Client.Id);
        var sut = fixture.Sut;

        var request = new ClientReportRequest(fixture.Board.Id, "lastVisit");
        var result = await sut.HandleAsync(request, CancellationToken.None);

        AssertSuccess(result);
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
        var fixture = Fixture.Init(DbContext, Db).WithSeededEntities();
        fixture.WithAppointment(fixture.Saturday, 100, "Room 1", fixture.Client.Id,
            new DateTime(2026, 5, 30, 10, 0, 0, DateTimeKind.Utc));
        fixture.WithAppointment(fixture.Saturday.AddDays(7), 150, "Room 1", fixture.Client.Id,
            new DateTime(2026, 6, 6, 10, 0, 0, DateTimeKind.Utc));
        var sut = fixture.Sut;

        var request = new ClientReportRequest(fixture.Board.Id, "totalSpent");
        var result = await sut.HandleAsync(request, CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().ContainSingle();
        var report = result.Value.Single();
        report.ClientName.Should().Be("Alice");
        report.VisitCount.Should().Be(2);
        report.TotalSpent.Should().Be(250);
        report.AveragePerVisit.Should().Be(125);
    }
}
