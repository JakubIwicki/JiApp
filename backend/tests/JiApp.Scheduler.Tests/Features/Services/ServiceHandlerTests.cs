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

public sealed class ServiceHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly Mock<ICurrentUserService> _currentUser;
    private readonly SchedulerDbContext _db;

    public ServiceHandlerTests()
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

    public void Dispose()
    {
        _db.Dispose();
        _connection.Close();
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
        var board = new Board { Name = "Board", MemberUserIds = [1L] };
        _db.Boards.Add(board);
        await _db.SaveChangesAsync();

        var handler = new CreateServiceHandler(_db, _currentUser.Object);
        var request = new CreateServiceRequest(board.Id, "Haircut", "MensHaircut", 30, new PriceRequest(100));

        var result = await handler.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateService_WithInvalidCategory_ReturnsFailure()
    {
        var board = new Board { Name = "Board", MemberUserIds = [1L] };
        _db.Boards.Add(board);
        await _db.SaveChangesAsync();

        var handler = new CreateServiceHandler(_db, _currentUser.Object);
        var request = new CreateServiceRequest(board.Id, "Haircut", "InvalidCategory", 30, new PriceRequest(100));

        var result = await handler.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid service category: InvalidCategory");
    }

    [Fact]
    public async Task ListServices_WithoutFilters_ReturnsAll()
    {
        var board = new Board { Name = "Board", MemberUserIds = [1L] };
        _db.Boards.Add(board);
        await _db.SaveChangesAsync();
        _db.Services.AddRange(
            new Service
            {
                Name = "Cut", Category = ServiceCategory.MensHaircut, BaseDuration = 30, BasePrice = new Price(100),
                Board = board
            },
            new Service
            {
                Name = "Wash", Category = ServiceCategory.WomensHaircut, BaseDuration = 45, BasePrice = new Price(150),
                Board = board
            });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new ListServicesHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(board.Id, null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListServices_WithoutBoardId_ReturnsFailure()
    {
        var handler = new ListServicesHandler(_db, _currentUser.Object);

        var act = async () => await handler.HandleAsync(0, null, CancellationToken.None);

        var result = await handler.HandleAsync(0, null, CancellationToken.None);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ListServices_WithBoardFilter_FiltersByBoard()
    {
        var board1 = new Board { Name = "B1", MemberUserIds = [1L] };
        var board2 = new Board { Name = "B2", MemberUserIds = [1L] };
        _db.Boards.AddRange(board1, board2);
        await _db.SaveChangesAsync();
        _db.Services.AddRange(
            new Service
            {
                Name = "Cut", Category = ServiceCategory.MensHaircut, BaseDuration = 30, BasePrice = new Price(100),
                Board = board1
            },
            new Service
            {
                Name = "Color", Category = ServiceCategory.Coloring, BaseDuration = 60, BasePrice = new Price(200),
                Board = board2
            });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new ListServicesHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(board1.Id, null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(s => s.Name == "Cut");
    }

    [Fact]
    public async Task ListServices_WithCategoryFilter_FiltersByCategory()
    {
        var board = new Board { Name = "Board", MemberUserIds = [1L] };
        _db.Boards.Add(board);
        await _db.SaveChangesAsync();
        _db.Services.AddRange(
            new Service
            {
                Name = "Cut", Category = ServiceCategory.MensHaircut, BaseDuration = 30, BasePrice = new Price(100),
                Board = board
            },
            new Service
            {
                Name = "Style", Category = ServiceCategory.WomensStyling, BaseDuration = 45, BasePrice = new Price(180),
                Board = board
            });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new ListServicesHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(board.Id, "MensHaircut", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(s => s.Name == "Cut");
    }

    [Fact]
    public async Task GetService_WithValidId_ReturnsService()
    {
        var board = new Board { Name = "Board", MemberUserIds = [1L] };
        _db.Boards.Add(board);
        await _db.SaveChangesAsync();
        var service = new Service
        {
            Name = "Cut", Category = ServiceCategory.MensHaircut, BaseDuration = 30, BasePrice = new Price(100),
            Board = board
        };
        _db.Services.Add(service);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new GetServiceHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(service.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Cut");
        result.Value.BaseDuration.Should().Be(30);
        result.Value.Category.Should().Be("MensHaircut");
    }

    [Fact]
    public async Task GetService_WithInvalidId_ReturnsFailure()
    {
        var handler = new GetServiceHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(999L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateService_WithValidData_Updates()
    {
        var board = new Board { Name = "Board", MemberUserIds = [1L] };
        _db.Boards.Add(board);
        await _db.SaveChangesAsync();
        var service = new Service
        {
            Name = "Old", Category = ServiceCategory.MensHaircut, BaseDuration = 30, BasePrice = new Price(100),
            Board = board
        };
        _db.Services.Add(service);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new UpdateServiceHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(service.Id,
            new UpdateServiceRequest("New", "Coloring", 60, new PriceRequest(250)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var updated = await _db.Services.FindAsync(service.Id);
        updated!.Name.Should().Be("New");
        updated.Category.Should().Be(ServiceCategory.Coloring);
        updated.BaseDuration.Should().Be(60);
        updated.BasePrice.Amount.Should().Be(250);
    }

    [Fact]
    public async Task UpdateService_WithInvalidId_ReturnsFailure()
    {
        var handler = new UpdateServiceHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(999L,
            new UpdateServiceRequest("Test", "Other", 30, new PriceRequest(100)), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteService_WithNoAppointments_Deletes()
    {
        var board = new Board { Name = "Board", MemberUserIds = [1L] };
        _db.Boards.Add(board);
        await _db.SaveChangesAsync();
        var service = new Service
        {
            Name = "Cut", Category = ServiceCategory.MensHaircut, BaseDuration = 30, BasePrice = new Price(100),
            Board = board
        };
        _db.Services.Add(service);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new DeleteServiceHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(service.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var deleted = await _db.Services.FindAsync(service.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteService_WithExistingAppointments_ReturnsFailure()
    {
        var board = new Board { Name = "Board", MemberUserIds = [1L] };
        var client = new Client { Name = "Client", Board = board };
        var service = new Service
        {
            Name = "Cut", Category = ServiceCategory.MensHaircut, BaseDuration = 30, BasePrice = new Price(100),
            Board = board
        };
        var appointment = new Appointment
        {
            Board = board,
            Client = client,
            Service = service,
            Date = WeekendDate(),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0)
        };
        _db.Boards.Add(board);
        _db.Clients.Add(client);
        _db.Services.Add(service);
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new DeleteServiceHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(service.Id, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CreateService_WithInvalidCategory_ReturnsValidationErrorCategory()
    {
        var board = new Board { Name = "Board", MemberUserIds = [1L] };
        _db.Boards.Add(board);
        await _db.SaveChangesAsync();

        var handler = new CreateServiceHandler(_db, _currentUser.Object);
        var request = new CreateServiceRequest(board.Id, "Haircut", "InvalidCategory", 30, new PriceRequest(100));

        var result = await handler.HandleAsync(request, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.Validation);
    }

    [Fact]
    public async Task UpdateService_WithInvalidCategory_ReturnsValidationErrorCategory()
    {
        var board = new Board { Name = "Board", MemberUserIds = [1L] };
        _db.Boards.Add(board);
        await _db.SaveChangesAsync();
        var service = new Service
        {
            Name = "Old", Category = ServiceCategory.MensHaircut, BaseDuration = 30, BasePrice = new Price(100),
            Board = board
        };
        _db.Services.Add(service);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new UpdateServiceHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(service.Id,
            new UpdateServiceRequest("New", "InvalidCategory", 60, new PriceRequest(250)), CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.Validation);
    }

    [Fact]
    public async Task ListServices_WithInvalidBoard_ReturnsNotFoundErrorCategory()
    {
        var handler = new ListServicesHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(999L, null, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }

    [Fact]
    public async Task ListServices_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        var otherBoard = new Board { Name = "Other", MemberUserIds = [2L] };
        _db.Boards.Add(otherBoard);
        await _db.SaveChangesAsync();

        var handler = new ListServicesHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(otherBoard.Id, null, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.AccessDenied);
    }
}