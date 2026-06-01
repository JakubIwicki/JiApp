using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Clients.CreateClient;
using JiApp.Scheduler.Features.Clients.DeleteClient;
using JiApp.Scheduler.Features.Clients.GetClient;
using JiApp.Scheduler.Features.Clients.ListClients;
using JiApp.Scheduler.Features.Clients.UpdateClient;
using Microsoft.Data.Sqlite;

namespace JiApp.Scheduler.Tests.Features.Clients;

public sealed class ClientHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SchedulerDbContext _db;
    private readonly Mock<ICurrentUserService> _currentUser;

    public ClientHandlerTests()
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

    private Board CreateBoard()
    {
        var board = new Board { Name = "Test Board", MemberUserIds = [1L] };
        _db.Boards.Add(board);
        _db.SaveChanges();
        return board;
    }

    [Fact]
    public async Task CreateClient_WithValidData_ReturnsClientId()
    {
        var board = CreateBoard();
        var handler = new CreateClientHandler(_db, _currentUser.Object);
        var request = new CreateClientRequest(board.Id, "John Doe", "123456789", null);

        var result = await handler.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ListClients_WithoutSearch_ReturnsAll()
    {
        var board = CreateBoard();
        _db.Clients.AddRange(
            new Client { BoardId = board.Id, Name = "Alice" },
            new Client { BoardId = board.Id, Name = "Bob" });
        await _db.SaveChangesAsync();

        var handler = new ListClientsHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(null, 0, 50, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListClients_WithSearch_FiltersByName()
    {
        var board = CreateBoard();
        _db.Clients.AddRange(
            new Client { BoardId = board.Id, Name = "Alice" },
            new Client { BoardId = board.Id, Name = "Bob" });
        await _db.SaveChangesAsync();

        var handler = new ListClientsHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync("Ali", 0, 50, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(c => c.Name == "Alice");
    }

    [Fact]
    public async Task ListClients_WithPagination_SkipsAndTakes()
    {
        var board = CreateBoard();
        for (var i = 0; i < 10; i++)
            _db.Clients.Add(new Client { BoardId = board.Id, Name = $"Client{i}" });
        await _db.SaveChangesAsync();

        var handler = new ListClientsHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(null, 2, 3, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value[0].Name.Should().Be("Client2");
        result.Value[1].Name.Should().Be("Client3");
        result.Value[2].Name.Should().Be("Client4");
    }

    [Fact]
    public async Task ListClients_WithPagination_DefaultsToFirst50()
    {
        var board = CreateBoard();
        for (var i = 0; i < 60; i++)
            _db.Clients.Add(new Client { BoardId = board.Id, Name = $"Client{i}" });
        await _db.SaveChangesAsync();

        var handler = new ListClientsHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(null, 0, 50, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(50);
        result.Value[0].Name.Should().Be("Client0");
    }

    [Fact]
    public async Task GetClient_WithValidId_ReturnsClient()
    {
        var board = CreateBoard();
        var client = new Client { BoardId = board.Id, Name = "Alice", Phone = "123" };
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();

        var handler = new GetClientHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(client.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Alice");
        result.Value.Phone.Should().Be("123");
    }

    [Fact]
    public async Task GetClient_WithInvalidId_ReturnsFailure()
    {
        var handler = new GetClientHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(999L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateClient_WithValidData_UpdatesName()
    {
        var board = CreateBoard();
        var client = new Client { BoardId = board.Id, Name = "Original" };
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();

        var handler = new UpdateClientHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(client.Id, new UpdateClientRequest("Updated", null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var updated = await _db.Clients.FindAsync(client.Id);
        updated!.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task DeleteClient_WithNoAppointments_Deletes()
    {
        var board = CreateBoard();
        var client = new Client { BoardId = board.Id, Name = "Alice" };
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();

        var handler = new DeleteClientHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(client.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var deleted = await _db.Clients.FindAsync(client.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteClient_WithExistingAppointments_ReturnsFailure()
    {
        var board = CreateBoard();
        var client = new Client { BoardId = board.Id, Name = "Alice" };
        var service = new Service
            { Name = "Cut", BoardId = board.Id, Category = ServiceCategory.MensHaircut, BaseDuration = 30 };
        var appointment = new Appointment
        {
            BoardId = board.Id,
            Client = client,
            Service = service,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
        };

        _db.Clients.Add(client);
        _db.Services.Add(service);
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        _db.ChangeTracker.Clear();

        var handler = new DeleteClientHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(client.Id, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CreateClient_WithInvalidBoard_ReturnsNotFoundErrorCategory()
    {
        var handler = new CreateClientHandler(_db, _currentUser.Object);
        var request = new CreateClientRequest(999L, "John", null, null);

        var result = await handler.HandleAsync(request, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }

    [Fact]
    public async Task CreateClient_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        var board = new Board { Name = "Test", MemberUserIds = [2L] };
        _db.Boards.Add(board);
        await _db.SaveChangesAsync();

        var handler = new CreateClientHandler(_db, _currentUser.Object);
        var request = new CreateClientRequest(board.Id, "John", null, null);

        var result = await handler.HandleAsync(request, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.AccessDenied);
    }

    [Fact]
    public async Task GetClient_WithInvalidId_ReturnsNotFoundErrorCategory()
    {
        var handler = new GetClientHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(999L, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }

    [Fact]
    public async Task GetClient_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        var board = CreateBoard();
        var client = new Client { BoardId = board.Id, Name = "Alice" };
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();

        // Set current user to a non-member (userId is already 1 from Setup, and board's member is 1,
        // so use a board created by a different user)
        var otherBoard = new Board { Name = "Other", MemberUserIds = [2L] };
        _db.Boards.Add(otherBoard);
        await _db.SaveChangesAsync();

        var otherClient = new Client { BoardId = otherBoard.Id, Name = "Bob" };
        _db.Clients.Add(otherClient);
        await _db.SaveChangesAsync();

        // Current user (1L) is not a member of otherBoard (member is 2L)
        var handler = new GetClientHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(otherClient.Id, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.AccessDenied);
    }

    [Fact]
    public async Task UpdateClient_WithInvalidId_ReturnsNotFoundErrorCategory()
    {
        var handler = new UpdateClientHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(999L, new UpdateClientRequest("Updated", null, null),
            CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }

    [Fact]
    public async Task UpdateClient_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        var board = new Board { Name = "Other", MemberUserIds = [2L] };
        _db.Boards.Add(board);
        await _db.SaveChangesAsync();

        var client = new Client { BoardId = board.Id, Name = "Bob" };
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();

        var handler = new UpdateClientHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(client.Id, new UpdateClientRequest("Updated", null, null),
            CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.AccessDenied);
    }

    [Fact]
    public async Task DeleteClient_WithInvalidId_ReturnsNotFoundErrorCategory()
    {
        var handler = new DeleteClientHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(999L, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }

    [Fact]
    public async Task DeleteClient_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        var board = new Board { Name = "Other", MemberUserIds = [2L] };
        _db.Boards.Add(board);
        await _db.SaveChangesAsync();

        var client = new Client { BoardId = board.Id, Name = "Bob" };
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();

        var handler = new DeleteClientHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(client.Id, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.AccessDenied);
    }

    [Fact]
    public async Task DeleteClient_WithExistingAppointments_ReturnsConflictErrorCategory()
    {
        var board = CreateBoard();
        var client = new Client { BoardId = board.Id, Name = "Alice" };
        var service = new Service
            { Name = "Cut", BoardId = board.Id, Category = ServiceCategory.MensHaircut, BaseDuration = 30 };
        var appointment = new Appointment
        {
            BoardId = board.Id,
            Client = client,
            Service = service,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
        };

        _db.Clients.Add(client);
        _db.Services.Add(service);
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        _db.ChangeTracker.Clear();

        var handler = new DeleteClientHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(client.Id, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.Conflict);
    }
}