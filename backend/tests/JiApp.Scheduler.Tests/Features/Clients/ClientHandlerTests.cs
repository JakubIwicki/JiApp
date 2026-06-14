using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Clients.CreateClient;
using JiApp.Scheduler.Features.Clients.DeleteClient;
using JiApp.Scheduler.Features.Clients.GetClient;
using JiApp.Scheduler.Features.Clients.ListClients;
using JiApp.Scheduler.Features.Clients.UpdateClient;
using Microsoft.Data.Sqlite;

namespace JiApp.Scheduler.Tests.Features.Clients;

public sealed class ClientHandlerTests
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

        public Fixture WithBoard(string name = "Test Board", List<long>? memberUserIds = null)
        {
            var board = new Board { Name = name, MemberUserIds = memberUserIds ?? [1L] };
            _db.Boards.Add(board);
            _db.SaveChanges();
            return this;
        }

        public Fixture WithBoard(out long boardId, string name = "Test Board", List<long>? memberUserIds = null)
        {
            var board = new Board { Name = name, MemberUserIds = memberUserIds ?? [1L] };
            _db.Boards.Add(board);
            _db.SaveChanges();
            boardId = board.Id;
            return this;
        }

        public CreateClientHandler CreateClientSut => new(_db, _currentUser.Object);
        public GetClientHandler GetClientSut => new(_db, _currentUser.Object);
        public ListClientsHandler ListClientsSut => new(_db, _currentUser.Object);
        public UpdateClientHandler UpdateClientSut => new(_db, _currentUser.Object);
        public DeleteClientHandler DeleteClientSut => new(_db, _currentUser.Object);

        public void Dispose()
        {
            _db.Dispose();
            _connection.Close();
        }
    }

    [Fact]
    public async Task CreateClient_WithValidData_ReturnsClientId()
    {
        using var fixture = new Fixture().WithBoard(out var boardId);
        var sut = fixture.CreateClientSut;
        var request = new CreateClientRequest(boardId, "John Doe", "123456789", null);

        var result = await sut.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ListClients_WithoutSearch_ReturnsAll()
    {
        using var fixture = new Fixture().WithBoard(out var boardId);
        fixture.Db.Clients.AddRange(
            new Client { BoardId = boardId, Name = "Alice" },
            new Client { BoardId = boardId, Name = "Bob" });
        await fixture.Db.SaveChangesAsync();

        var sut = fixture.ListClientsSut;
        var result = await sut.HandleAsync(null, 0, 50, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListClients_WithSearch_FiltersByName()
    {
        using var fixture = new Fixture().WithBoard(out var boardId);
        fixture.Db.Clients.AddRange(
            new Client { BoardId = boardId, Name = "Alice" },
            new Client { BoardId = boardId, Name = "Bob" });
        await fixture.Db.SaveChangesAsync();

        var sut = fixture.ListClientsSut;
        var result = await sut.HandleAsync("Ali", 0, 50, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(c => c.Name == "Alice");
    }

    [Fact]
    public async Task ListClients_WithPagination_SkipsAndTakes()
    {
        using var fixture = new Fixture().WithBoard(out var boardId);
        for (var i = 0; i < 10; i++)
            fixture.Db.Clients.Add(new Client { BoardId = boardId, Name = $"Client{i}" });
        await fixture.Db.SaveChangesAsync();

        var sut = fixture.ListClientsSut;
        var result = await sut.HandleAsync(null, 2, 3, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value[0].Name.Should().Be("Client2");
        result.Value[1].Name.Should().Be("Client3");
        result.Value[2].Name.Should().Be("Client4");
    }

    [Fact]
    public async Task ListClients_WithPagination_DefaultsToFirst50()
    {
        using var fixture = new Fixture().WithBoard(out var boardId);
        for (var i = 0; i < 60; i++)
            fixture.Db.Clients.Add(new Client { BoardId = boardId, Name = $"Client{i}" });
        await fixture.Db.SaveChangesAsync();

        var sut = fixture.ListClientsSut;
        var result = await sut.HandleAsync(null, 0, 50, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(50);
        result.Value[0].Name.Should().Be("Client0");
    }

    [Fact]
    public async Task GetClient_WithValidId_ReturnsClient()
    {
        using var fixture = new Fixture().WithBoard(out var boardId);
        var client = new Client { BoardId = boardId, Name = "Alice", Phone = "123" };
        fixture.Db.Clients.Add(client);
        await fixture.Db.SaveChangesAsync();

        var sut = fixture.GetClientSut;
        var result = await sut.HandleAsync(client.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Alice");
        result.Value.Phone.Should().Be("123");
    }

    [Fact]
    public async Task GetClient_WithInvalidId_ReturnsFailure()
    {
        using var fixture = new Fixture();
        var sut = fixture.GetClientSut;
        var result = await sut.HandleAsync(999L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateClient_WithValidData_UpdatesName()
    {
        using var fixture = new Fixture().WithBoard(out var boardId);
        var client = new Client { BoardId = boardId, Name = "Original" };
        fixture.Db.Clients.Add(client);
        await fixture.Db.SaveChangesAsync();

        var sut = fixture.UpdateClientSut;
        var result = await sut.HandleAsync(client.Id, new UpdateClientRequest("Updated", null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var updated = await fixture.Db.Clients.FindAsync(client.Id);
        updated!.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task DeleteClient_WithNoAppointments_Deletes()
    {
        using var fixture = new Fixture().WithBoard(out var boardId);
        var client = new Client { BoardId = boardId, Name = "Alice" };
        fixture.Db.Clients.Add(client);
        await fixture.Db.SaveChangesAsync();

        var sut = fixture.DeleteClientSut;
        var result = await sut.HandleAsync(client.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var deleted = await fixture.Db.Clients.FindAsync(client.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteClient_WithExistingAppointments_ReturnsFailure()
    {
        using var fixture = new Fixture().WithBoard(out var boardId);
        var client = new Client { BoardId = boardId, Name = "Alice" };
        var service = new Service
            { Name = "Cut", BoardId = boardId, Category = ServiceCategory.MensHaircut, BaseDuration = 30 };
        var appointment = new Appointment
        {
            BoardId = boardId,
            Client = client,
            Service = service,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
        };

        fixture.Db.Clients.Add(client);
        fixture.Db.Services.Add(service);
        fixture.Db.Appointments.Add(appointment);
        await fixture.Db.SaveChangesAsync();
        fixture.Db.ChangeTracker.Clear();

        var sut = fixture.DeleteClientSut;
        var result = await sut.HandleAsync(client.Id, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CreateClient_WithInvalidBoard_ReturnsNotFoundErrorCategory()
    {
        using var fixture = new Fixture();
        var sut = fixture.CreateClientSut;
        var request = new CreateClientRequest(999L, "John", null, null);

        var result = await sut.HandleAsync(request, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }

    [Fact]
    public async Task CreateClient_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        using var fixture = new Fixture().WithBoard(out var boardId, memberUserIds: [2L]);
        var sut = fixture.CreateClientSut;
        var request = new CreateClientRequest(boardId, "John", null, null);

        var result = await sut.HandleAsync(request, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.AccessDenied);
    }

    [Fact]
    public async Task GetClient_WithInvalidId_ReturnsNotFoundErrorCategory()
    {
        using var fixture = new Fixture();
        var sut = fixture.GetClientSut;
        var result = await sut.HandleAsync(999L, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }

    [Fact]
    public async Task GetClient_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        using var fixture = new Fixture();
        // Create a board for a different user so current user is not a member
        var otherBoard = new Board { Name = "Other", MemberUserIds = [2L] };
        fixture.Db.Boards.Add(otherBoard);
        await fixture.Db.SaveChangesAsync();
        var otherClient = new Client { BoardId = otherBoard.Id, Name = "Bob" };
        fixture.Db.Clients.Add(otherClient);
        await fixture.Db.SaveChangesAsync();

        var sut = fixture.GetClientSut;
        var result = await sut.HandleAsync(otherClient.Id, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.AccessDenied);
    }

    [Fact]
    public async Task UpdateClient_WithInvalidId_ReturnsNotFoundErrorCategory()
    {
        using var fixture = new Fixture();
        var sut = fixture.UpdateClientSut;
        var result = await sut.HandleAsync(999L, new UpdateClientRequest("Updated", null, null),
            CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }

    [Fact]
    public async Task UpdateClient_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        using var fixture = new Fixture().WithBoard(out var boardId, memberUserIds: [2L]);
        var client = new Client { BoardId = boardId, Name = "Bob" };
        fixture.Db.Clients.Add(client);
        await fixture.Db.SaveChangesAsync();

        var sut = fixture.UpdateClientSut;
        var result = await sut.HandleAsync(client.Id, new UpdateClientRequest("Updated", null, null),
            CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.AccessDenied);
    }

    [Fact]
    public async Task DeleteClient_WithInvalidId_ReturnsNotFoundErrorCategory()
    {
        using var fixture = new Fixture();
        var sut = fixture.DeleteClientSut;
        var result = await sut.HandleAsync(999L, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }

    [Fact]
    public async Task DeleteClient_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        using var fixture = new Fixture().WithBoard(out var boardId, memberUserIds: [2L]);
        var client = new Client { BoardId = boardId, Name = "Bob" };
        fixture.Db.Clients.Add(client);
        await fixture.Db.SaveChangesAsync();

        var sut = fixture.DeleteClientSut;
        var result = await sut.HandleAsync(client.Id, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.AccessDenied);
    }

    [Fact]
    public async Task DeleteClient_WithExistingAppointments_ReturnsConflictErrorCategory()
    {
        using var fixture = new Fixture().WithBoard(out var boardId);
        var client = new Client { BoardId = boardId, Name = "Alice" };
        var service = new Service
            { Name = "Cut", BoardId = boardId, Category = ServiceCategory.MensHaircut, BaseDuration = 30 };
        var appointment = new Appointment
        {
            BoardId = boardId,
            Client = client,
            Service = service,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
        };

        fixture.Db.Clients.Add(client);
        fixture.Db.Services.Add(service);
        fixture.Db.Appointments.Add(appointment);
        await fixture.Db.SaveChangesAsync();
        fixture.Db.ChangeTracker.Clear();

        var sut = fixture.DeleteClientSut;
        var result = await sut.HandleAsync(client.Id, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.Conflict);
    }
}
