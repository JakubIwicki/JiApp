using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Features.Reports.RevenueReport;

namespace JiApp.Scheduler.Tests.Features.Reports;

public sealed class RevenueReportHandlerTests : HandlerTestBase
{
    private sealed class Fixture
    {
        private readonly ISchedulerDbContext _dbContext;
        private readonly SchedulerDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly DateOnly _saturday;
        private readonly DateOnly _sunday;
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
            _sunday = _saturday.AddDays(1);
        }

        public SchedulerDbContext Db => _db;
        public Board Board => _board!;
        public Client Client => _client!;
        public Service Service => _service!;
        public DateOnly Saturday => _saturday;
        public DateOnly Sunday => _sunday;

        public RevenueReportHandler Sut => new(_dbContext, _currentUser);

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

        public Fixture WithService(string name, string category, int duration, decimal price)
        {
            var service = new Service
            {
                Name = name,
                Category = Enum.Parse<ServiceCategory>(category),
                BaseDuration = duration,
                BasePrice = new Price(price),
                BoardId = _board!.Id
            };
            _db.Services.Add(service);
            _db.SaveChanges();
            _db.ChangeTracker.Clear();
            return this;
        }

        public Fixture WithAppointment(DateOnly date, decimal price, string location, long? serviceId = null)
        {
            var appointment = new Appointment
            {
                BoardId = _board!.Id, ClientId = _client!.Id, ServiceId = serviceId ?? _service!.Id,
                Date = date, StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0),
                Price = new Price(price), Location = location,
                CreatedBy = 1L
            };
            _db.Appointments.Add(appointment);
            _db.SaveChanges();
            _db.ChangeTracker.Clear();
            return this;
        }

        public Fixture WithBoard(string name = "Other Board", List<long>? memberUserIds = null)
        {
            var board = new Board { Name = name, MemberUserIds = memberUserIds ?? [1L] };
            _db.Boards.Add(board);
            _db.SaveChanges();
            _db.ChangeTracker.Clear();
            return this;
        }
    }

    [Fact]
    public async Task RevenueReport_InvalidGroupBy_ReturnsFailure()
    {
        var fixture = Fixture.Init(DbContext, Db).WithSeededEntities();
        var sut = fixture.Sut;
        var request = new RevenueReportRequest(fixture.Board.Id, fixture.Saturday, fixture.Sunday, "invalid");

        var result = await sut.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid groupBy. Use: weekend, service, location, or client");
    }

    [Fact]
    public async Task RevenueReport_FromAfterTo_ReturnsFailure()
    {
        var fixture = Fixture.Init(DbContext, Db).WithSeededEntities();
        var sut = fixture.Sut;
        var request = new RevenueReportRequest(fixture.Board.Id, fixture.Sunday, fixture.Saturday, "weekend");

        var result = await sut.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("From date must be before or equal to To date");
    }

    [Fact]
    public async Task RevenueReport_ByService_ReturnsGroupedData()
    {
        var fixture = Fixture.Init(DbContext, Db).WithSeededEntities()
            .WithService("Coloring", "Coloring", 60, 300);
        var service2 = fixture.Db.Services.Single(s => s.Name == "Coloring");

        fixture.WithAppointment(fixture.Saturday, 100, "Room 1");
        fixture.WithAppointment(fixture.Saturday, 300, "Room 2", service2.Id);
        var sut = fixture.Sut;

        var request = new RevenueReportRequest(fixture.Board.Id, fixture.Saturday, fixture.Saturday.AddDays(1), "service");
        var result = await sut.HandleAsync(request, CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().HaveCount(2);
        var haircut = result.Value.Single(r => r.GroupKey == "Haircut");
        haircut.Revenue.Should().Be(100);
        haircut.AppointmentCount.Should().Be(1);
    }

    [Fact]
    public async Task RevenueReport_ByLocation_ReturnsGroupedData()
    {
        var fixture = Fixture.Init(DbContext, Db).WithSeededEntities();
        fixture.WithAppointment(fixture.Saturday, 100, "Room 1");
        fixture.WithAppointment(fixture.Saturday, 150, "Room 2");
        var sut = fixture.Sut;

        var request = new RevenueReportRequest(fixture.Board.Id, fixture.Saturday, fixture.Saturday.AddDays(1), "location");
        var result = await sut.HandleAsync(request, CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(r => r.GroupKey == "Room 1");
        result.Value.Should().Contain(r => r.GroupKey == "Room 2");
    }

    [Fact]
    public async Task RevenueReport_ByClient_ReturnsGroupedData()
    {
        var fixture = Fixture.Init(DbContext, Db).WithSeededEntities();
        fixture.WithAppointment(fixture.Saturday, 100, "Room 1");
        var sut = fixture.Sut;

        var request = new RevenueReportRequest(fixture.Board.Id, fixture.Saturday, fixture.Saturday.AddDays(1), "client");
        var result = await sut.HandleAsync(request, CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().ContainSingle(r => r.GroupKey == "Alice");
    }

    [Fact]
    public async Task RevenueReport_ByWeekend_GroupsSatAndSunTogether()
    {
        var fixture = Fixture.Init(DbContext, Db).WithSeededEntities();
        fixture.WithAppointment(fixture.Saturday, 200, "Room 1");
        fixture.WithAppointment(fixture.Sunday, 300, "Room 2");
        var sut = fixture.Sut;

        var request = new RevenueReportRequest(fixture.Board.Id, fixture.Saturday, fixture.Sunday, "weekend");
        var result = await sut.HandleAsync(request, CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().ContainSingle();
        var weekend = result.Value.Single();
        weekend.Revenue.Should().Be(500);
        weekend.AppointmentCount.Should().Be(2);
        weekend.Net.Should().Be(500);
    }

    [Fact]
    public async Task RevenueReport_InvalidGroupBy_ReturnsValidationErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db).WithSeededEntities();
        var sut = fixture.Sut;
        var request = new RevenueReportRequest(fixture.Board.Id, fixture.Saturday, fixture.Sunday, "invalid");

        var result = await sut.HandleAsync(request, CancellationToken.None);

        AssertValidationFailure(result);
    }

    [Fact]
    public async Task RevenueReport_FromAfterTo_ReturnsValidationErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db).WithSeededEntities();
        var sut = fixture.Sut;
        var request = new RevenueReportRequest(fixture.Board.Id, fixture.Sunday, fixture.Saturday, "weekend");

        var result = await sut.HandleAsync(request, CancellationToken.None);

        AssertValidationFailure(result);
    }

    [Fact]
    public async Task RevenueReport_WithInvalidBoard_ReturnsNotFoundErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db).WithSeededEntities();
        var sut = fixture.Sut;
        var request = new RevenueReportRequest(999L, fixture.Saturday, fixture.Sunday, "weekend");

        var result = await sut.HandleAsync(request, CancellationToken.None);

        AssertNotFound(result);
    }

    [Fact]
    public async Task RevenueReport_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db).WithSeededEntities();
        fixture.WithBoard(name: "Other", memberUserIds: [2L]);
        var otherBoard = fixture.Db.Boards.Single(b => b.Name == "Other");
        var sut = fixture.Sut;
        var request = new RevenueReportRequest(otherBoard.Id, fixture.Saturday, fixture.Sunday, "weekend");

        var result = await sut.HandleAsync(request, CancellationToken.None);

        AssertAccessDenied(result);
    }
}
