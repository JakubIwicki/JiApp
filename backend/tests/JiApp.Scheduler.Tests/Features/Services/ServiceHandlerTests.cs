using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Features.Services.CreateService;
using JiApp.Scheduler.Features.Services.DeleteService;
using JiApp.Scheduler.Features.Services.GetService;
using JiApp.Scheduler.Features.Services.ListServices;
using JiApp.Scheduler.Features.Services.UpdateService;

namespace JiApp.Scheduler.Tests.Features.Services;

public sealed class ServiceHandlerTests : HandlerTestBase
{
    private sealed class Fixture
    {
        private readonly ISchedulerDbContext _dbContext;
        private readonly SchedulerDbContext _db;
        private readonly TestDb _testDb;
        private readonly ICurrentUserService _currentUser;

        private Fixture(ISchedulerDbContext dbContext, TestDb testDb)
        {
            _dbContext = dbContext;
            _db = (SchedulerDbContext)dbContext;
            _testDb = testDb;
            _currentUser = MockCurrentUserService.GetSuccessful().Mock.Object;
        }

        public CreateServiceHandler Sut => new(_dbContext, _currentUser);
        public GetServiceHandler GetService => new(_dbContext, _currentUser);
        public ListServicesHandler ListServices => new(_dbContext, _currentUser);
        public UpdateServiceHandler UpdateService => new(_dbContext, _currentUser);
        public DeleteServiceHandler DeleteService => new(_dbContext, _currentUser);

        public static Fixture Init(ISchedulerDbContext dbContext, TestDb testDb) => new(dbContext, testDb);

        public Fixture WithBoard(string name = "Board", List<long>? memberUserIds = null)
        {
            var board = new Board { Name = name, MemberUserIds = memberUserIds ?? [1L] };
            _testDb.Store(board);
            return this;
        }

        public Fixture WithService(string name, string category, int duration, decimal price, Board board)
        {
            var service = new Service
            {
                Name = name,
                Category = Enum.Parse<ServiceCategory>(category),
                BaseDuration = duration,
                BasePrice = new Price(price),
                BoardId = board.Id
            };
            _db.Services.Add(service);
            _db.SaveChanges();
            _db.ChangeTracker.Clear();
            return this;
        }

        public Fixture WithAppointment(Board board, Client client, Service service, DateOnly date, TimeOnly start, TimeOnly end)
        {
            var appointment = new Appointment
            {
                Board = board,
                Client = client,
                Service = service,
                Date = date,
                StartTime = start,
                EndTime = end
            };
            _db.Attach(board);
            _db.Attach(client);
            _db.Attach(service);
            _db.Appointments.Add(appointment);
            _db.SaveChanges();
            _db.ChangeTracker.Clear();
            return this;
        }

        public Fixture WithClient(Client client)
        {
            _testDb.Store(client);
            return this;
        }
    }

    private static DateOnly WeekendDate()
    {
        var d = DateOnly.FromDateTime(DateTime.UtcNow);
        while (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
            d = d.AddDays(1);
        return d;
    }

    [Fact]
    public async Task CreateService_WithValidData_ReturnsServiceId()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard();
        var board = Db.Query<Board>().Single();
        var sut = fixture.Sut;
        var request = new CreateServiceRequest(board.Id, "Haircut", "MensHaircut", 30, new PriceRequest(100));

        var result = await sut.HandleAsync(request, CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateService_WithInvalidCategory_ReturnsFailure()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard();
        var board = Db.Query<Board>().Single();
        var sut = fixture.Sut;
        var request = new CreateServiceRequest(board.Id, "Haircut", "InvalidCategory", 30, new PriceRequest(100));

        var result = await sut.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid service category: InvalidCategory");
    }

    [Fact]
    public async Task ListServices_WithoutFilters_ReturnsAll()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard();
        var board = Db.Query<Board>().Single();
        fixture.WithService("Cut", "MensHaircut", 30, 100, board);
        fixture.WithService("Wash", "WomensHaircut", 45, 150, board);
        var sut = fixture.ListServices;
        var result = await sut.HandleAsync(board.Id, null, CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListServices_WithoutBoardId_ReturnsFailure()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.ListServices;

        var result = await sut.HandleAsync(0, null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ListServices_WithBoardFilter_FiltersByBoard()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var board1 = new Board { Name = "B1", MemberUserIds = [1L] };
        var board2 = new Board { Name = "B2", MemberUserIds = [1L] };
        StoreInDb(board1);
        StoreInDb(board2);

        fixture.WithService("Cut", "MensHaircut", 30, 100, board1);
        fixture.WithService("Color", "Coloring", 60, 200, board2);
        var sut = fixture.ListServices;
        var result = await sut.HandleAsync(board1.Id, null, CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().ContainSingle(s => s.Name == "Cut");
    }

    [Fact]
    public async Task ListServices_WithCategoryFilter_FiltersByCategory()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard();
        var board = Db.Query<Board>().Single();
        fixture.WithService("Cut", "MensHaircut", 30, 100, board);
        fixture.WithService("Style", "WomensStyling", 45, 180, board);
        var sut = fixture.ListServices;
        var result = await sut.HandleAsync(board.Id, "MensHaircut", CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().ContainSingle(s => s.Name == "Cut");
    }

    [Fact]
    public async Task GetService_WithValidId_ReturnsService()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard();
        var board = Db.Query<Board>().Single();
        fixture.WithService("Cut", "MensHaircut", 30, 100, board);
        var service = Db.Query<Service>().Single();
        var sut = fixture.GetService;
        var result = await sut.HandleAsync(service.Id, CancellationToken.None);

        AssertSuccess(result);
        result.Value!.Name.Should().Be("Cut");
        result.Value.BaseDuration.Should().Be(30);
        result.Value.Category.Should().Be("MensHaircut");
    }

    [Fact]
    public async Task GetService_WithInvalidId_ReturnsFailure()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.GetService;
        var result = await sut.HandleAsync(999L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateService_WithValidData_Updates()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard();
        var board = Db.Query<Board>().Single();
        fixture.WithService("Old", "MensHaircut", 30, 100, board);
        var service = Db.Query<Service>().Single();
        var sut = fixture.UpdateService;
        var result = await sut.HandleAsync(service.Id,
            new UpdateServiceRequest("New", "Coloring", 60, new PriceRequest(250)), CancellationToken.None);

        AssertSuccess(result);
        var updated = Db.Find<Service>(service.Id);
        updated!.Name.Should().Be("New");
        updated.Category.Should().Be(ServiceCategory.Coloring);
        updated.BaseDuration.Should().Be(60);
        updated.BasePrice.Amount.Should().Be(250);
    }

    [Fact]
    public async Task UpdateService_WithInvalidId_ReturnsFailure()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.UpdateService;
        var result = await sut.HandleAsync(999L,
            new UpdateServiceRequest("Test", "Other", 30, new PriceRequest(100)), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteService_WithNoAppointments_Deletes()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard();
        var board = Db.Query<Board>().Single();
        fixture.WithService("Cut", "MensHaircut", 30, 100, board);
        var service = Db.Query<Service>().Single();
        var sut = fixture.DeleteService;
        var result = await sut.HandleAsync(service.Id, CancellationToken.None);

        AssertSuccess(result);
        var deleted = Db.Find<Service>(service.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteService_WithExistingAppointments_ReturnsFailure()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard();
        var board = Db.Query<Board>().Single();
        var client = new Client { Name = "Client", BoardId = board.Id };
        fixture.WithClient(client);
        fixture.WithService("Cut", "MensHaircut", 30, 100, board);
        var service = Db.Query<Service>().Single();
        fixture.WithAppointment(board, client, service, WeekendDate(), new TimeOnly(10, 0), new TimeOnly(11, 0));
        var sut = fixture.DeleteService;
        var result = await sut.HandleAsync(service.Id, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CreateService_WithInvalidCategory_ReturnsValidationErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard();
        var board = Db.Query<Board>().Single();
        var sut = fixture.Sut;
        var request = new CreateServiceRequest(board.Id, "Haircut", "InvalidCategory", 30, new PriceRequest(100));

        var result = await sut.HandleAsync(request, CancellationToken.None);

        AssertValidationFailure(result);
    }

    [Fact]
    public async Task UpdateService_WithInvalidCategory_ReturnsValidationErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard();
        var board = Db.Query<Board>().Single();
        fixture.WithService("Old", "MensHaircut", 30, 100, board);
        var service = Db.Query<Service>().Single();
        var sut = fixture.UpdateService;
        var result = await sut.HandleAsync(service.Id,
            new UpdateServiceRequest("New", "InvalidCategory", 60, new PriceRequest(250)), CancellationToken.None);

        AssertValidationFailure(result);
    }

    [Fact]
    public async Task ListServices_WithInvalidBoard_ReturnsNotFoundErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.ListServices;
        var result = await sut.HandleAsync(999L, null, CancellationToken.None);

        AssertNotFound(result);
    }

    [Fact]
    public async Task ListServices_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(name: "Other", memberUserIds: [2L]);
        var otherBoard = Db.Query<Board>().Single();
        var sut = fixture.ListServices;
        var result = await sut.HandleAsync(otherBoard.Id, null, CancellationToken.None);

        AssertAccessDenied(result);
    }
}
