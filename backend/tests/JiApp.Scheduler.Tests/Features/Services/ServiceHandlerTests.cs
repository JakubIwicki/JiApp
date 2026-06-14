using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Features.Services.CreateService;
using JiApp.Scheduler.Features.Services.DeleteService;
using JiApp.Scheduler.Features.Services.GetService;
using JiApp.Scheduler.Features.Services.ListServices;
using JiApp.Scheduler.Features.Services.UpdateService;
using Microsoft.Data.Sqlite;

namespace JiApp.Scheduler.Tests.Features.Services;

public sealed class ServiceHandlerTests
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

        public Fixture WithService(string name, string category, int duration, decimal price, Board board)
        {
            var service = new Service
            {
                Name = name,
                Category = Enum.Parse<ServiceCategory>(category),
                BaseDuration = duration,
                BasePrice = new Price(price),
                Board = board
            };
            _db.Attach(board);
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
            _db.Clients.Add(client);
            _db.SaveChanges();
            _db.ChangeTracker.Clear();
            return this;
        }

        public CreateServiceHandler CreateServiceSut => new(_db, _currentUser.Object);
        public GetServiceHandler GetServiceSut => new(_db, _currentUser.Object);
        public ListServicesHandler ListServicesSut => new(_db, _currentUser.Object);
        public UpdateServiceHandler UpdateServiceSut => new(_db, _currentUser.Object);
        public DeleteServiceHandler DeleteServiceSut => new(_db, _currentUser.Object);

        public void Dispose()
        {
            _db.Dispose();
            _connection.Close();
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
        using var fixture = new Fixture().WithBoard();
        var board = fixture.Db.Boards.Single();
        var sut = fixture.CreateServiceSut;
        var request = new CreateServiceRequest(board.Id, "Haircut", "MensHaircut", 30, new PriceRequest(100));

        var result = await sut.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateService_WithInvalidCategory_ReturnsFailure()
    {
        using var fixture = new Fixture().WithBoard();
        var board = fixture.Db.Boards.Single();
        var sut = fixture.CreateServiceSut;
        var request = new CreateServiceRequest(board.Id, "Haircut", "InvalidCategory", 30, new PriceRequest(100));

        var result = await sut.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid service category: InvalidCategory");
    }

    [Fact]
    public async Task ListServices_WithoutFilters_ReturnsAll()
    {
        using var fixture = new Fixture().WithBoard();
        var board = fixture.Db.Boards.Single();
        fixture.WithService("Cut", "MensHaircut", 30, 100, board);
        fixture.WithService("Wash", "WomensHaircut", 45, 150, board);
        var sut = fixture.ListServicesSut;
        var result = await sut.HandleAsync(board.Id, null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListServices_WithoutBoardId_ReturnsFailure()
    {
        using var fixture = new Fixture();
        var sut = fixture.ListServicesSut;

        var result = await sut.HandleAsync(0, null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ListServices_WithBoardFilter_FiltersByBoard()
    {
        using var fixture = new Fixture();
        var board1 = new Board { Name = "B1", MemberUserIds = [1L] };
        var board2 = new Board { Name = "B2", MemberUserIds = [1L] };
        fixture.Db.Boards.AddRange(board1, board2);
        fixture.Db.SaveChanges();
        fixture.Db.ChangeTracker.Clear();

        fixture.WithService("Cut", "MensHaircut", 30, 100, board1);
        fixture.WithService("Color", "Coloring", 60, 200, board2);
        var sut = fixture.ListServicesSut;
        var result = await sut.HandleAsync(board1.Id, null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(s => s.Name == "Cut");
    }

    [Fact]
    public async Task ListServices_WithCategoryFilter_FiltersByCategory()
    {
        using var fixture = new Fixture().WithBoard();
        var board = fixture.Db.Boards.Single();
        fixture.WithService("Cut", "MensHaircut", 30, 100, board);
        fixture.WithService("Style", "WomensStyling", 45, 180, board);
        var sut = fixture.ListServicesSut;
        var result = await sut.HandleAsync(board.Id, "MensHaircut", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(s => s.Name == "Cut");
    }

    [Fact]
    public async Task GetService_WithValidId_ReturnsService()
    {
        using var fixture = new Fixture().WithBoard();
        var board = fixture.Db.Boards.Single();
        fixture.WithService("Cut", "MensHaircut", 30, 100, board);
        var service = fixture.Db.Services.Single();
        var sut = fixture.GetServiceSut;
        var result = await sut.HandleAsync(service.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Cut");
        result.Value.BaseDuration.Should().Be(30);
        result.Value.Category.Should().Be("MensHaircut");
    }

    [Fact]
    public async Task GetService_WithInvalidId_ReturnsFailure()
    {
        using var fixture = new Fixture();
        var sut = fixture.GetServiceSut;
        var result = await sut.HandleAsync(999L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateService_WithValidData_Updates()
    {
        using var fixture = new Fixture().WithBoard();
        var board = fixture.Db.Boards.Single();
        fixture.WithService("Old", "MensHaircut", 30, 100, board);
        var service = fixture.Db.Services.Single();
        var sut = fixture.UpdateServiceSut;
        var result = await sut.HandleAsync(service.Id,
            new UpdateServiceRequest("New", "Coloring", 60, new PriceRequest(250)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var updated = await fixture.Db.Services.FindAsync(service.Id);
        updated!.Name.Should().Be("New");
        updated.Category.Should().Be(ServiceCategory.Coloring);
        updated.BaseDuration.Should().Be(60);
        updated.BasePrice.Amount.Should().Be(250);
    }

    [Fact]
    public async Task UpdateService_WithInvalidId_ReturnsFailure()
    {
        using var fixture = new Fixture();
        var sut = fixture.UpdateServiceSut;
        var result = await sut.HandleAsync(999L,
            new UpdateServiceRequest("Test", "Other", 30, new PriceRequest(100)), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteService_WithNoAppointments_Deletes()
    {
        using var fixture = new Fixture().WithBoard();
        var board = fixture.Db.Boards.Single();
        fixture.WithService("Cut", "MensHaircut", 30, 100, board);
        var service = fixture.Db.Services.Single();
        var sut = fixture.DeleteServiceSut;
        var result = await sut.HandleAsync(service.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var deleted = await fixture.Db.Services.FindAsync(service.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteService_WithExistingAppointments_ReturnsFailure()
    {
        using var fixture = new Fixture().WithBoard();
        var board = fixture.Db.Boards.Single();
        var client = new Client { Name = "Client", Board = board };
        fixture.WithClient(client);
        fixture.WithService("Cut", "MensHaircut", 30, 100, board);
        var service = fixture.Db.Services.Single();
        fixture.WithAppointment(board, client, service, WeekendDate(), new TimeOnly(10, 0), new TimeOnly(11, 0));
        var sut = fixture.DeleteServiceSut;
        var result = await sut.HandleAsync(service.Id, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CreateService_WithInvalidCategory_ReturnsValidationErrorCategory()
    {
        using var fixture = new Fixture().WithBoard();
        var board = fixture.Db.Boards.Single();
        var sut = fixture.CreateServiceSut;
        var request = new CreateServiceRequest(board.Id, "Haircut", "InvalidCategory", 30, new PriceRequest(100));

        var result = await sut.HandleAsync(request, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.Validation);
    }

    [Fact]
    public async Task UpdateService_WithInvalidCategory_ReturnsValidationErrorCategory()
    {
        using var fixture = new Fixture().WithBoard();
        var board = fixture.Db.Boards.Single();
        fixture.WithService("Old", "MensHaircut", 30, 100, board);
        var service = fixture.Db.Services.Single();
        var sut = fixture.UpdateServiceSut;
        var result = await sut.HandleAsync(service.Id,
            new UpdateServiceRequest("New", "InvalidCategory", 60, new PriceRequest(250)), CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.Validation);
    }

    [Fact]
    public async Task ListServices_WithInvalidBoard_ReturnsNotFoundErrorCategory()
    {
        using var fixture = new Fixture();
        var sut = fixture.ListServicesSut;
        var result = await sut.HandleAsync(999L, null, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }

    [Fact]
    public async Task ListServices_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        using var fixture = new Fixture().WithBoard(name: "Other", memberUserIds: [2L]);
        var otherBoard = fixture.Db.Boards.Single();
        var sut = fixture.ListServicesSut;
        var result = await sut.HandleAsync(otherBoard.Id, null, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.AccessDenied);
    }
}
