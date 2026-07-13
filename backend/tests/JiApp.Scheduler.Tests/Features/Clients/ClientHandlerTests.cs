using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Clients.CreateClient;
using JiApp.Scheduler.Features.Clients.DeleteClient;
using JiApp.Scheduler.Features.Clients.GetClient;
using JiApp.Scheduler.Features.Clients.ListClients;
using JiApp.Scheduler.Features.Clients.UpdateClient;

namespace JiApp.Scheduler.Tests.Features.Clients;

public sealed class ClientHandlerTests : HandlerTestBase
{
    private sealed class Fixture
    {
        private readonly ISchedulerDbContext _dbContext;
        private readonly TestDb _testDb;
        private readonly ICurrentUserService _currentUser;

        private Fixture(ISchedulerDbContext dbContext, TestDb testDb)
        {
            _dbContext = dbContext;
            _testDb = testDb;
            _currentUser = MockCurrentUserService.GetSuccessful().Mock.Object;
        }

        public CreateClientHandler Sut => new(_dbContext, _currentUser);
        public CreateClientHandler CreateClient => new(_dbContext, _currentUser);
        public GetClientHandler GetClient => new(_dbContext, _currentUser);
        public ListClientsHandler ListClients => new(_dbContext, _currentUser);
        public UpdateClientHandler UpdateClient => new(_dbContext, _currentUser);
        public DeleteClientHandler DeleteClient => new(_dbContext, _currentUser);

        public static Fixture Init(ISchedulerDbContext dbContext, TestDb testDb) => new(dbContext, testDb);

        public Fixture WithBoard(string name = "Test Board", List<long>? memberUserIds = null)
        {
            var board = new Board { Name = name, MemberUserIds = memberUserIds ?? [1L] };
            _testDb.Store(board);
            return this;
        }

        public Fixture WithBoard(out long boardId, string name = "Test Board", List<long>? memberUserIds = null)
        {
            var board = new Board { Name = name, MemberUserIds = memberUserIds ?? [1L] };
            _testDb.Store(board);
            boardId = board.Id;
            return this;
        }
    }

    [Fact]
    public async Task CreateClient_WithValidData_ReturnsClientId()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        var sut = fixture.Sut;
        var request = new CreateClientRequest(boardId, "John Doe", "123456789", null);

        var result = await sut.HandleAsync(request, CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ListClients_WithoutSearch_ReturnsAll()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        StoreInDb(new Client { BoardId = boardId, Name = "Alice" });
        StoreInDb(new Client { BoardId = boardId, Name = "Bob" });

        var sut = fixture.ListClients;
        var result = await sut.HandleAsync(null, 0, 50, CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListClients_WithSearch_FiltersByName()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        StoreInDb(new Client { BoardId = boardId, Name = "Alice" });
        StoreInDb(new Client { BoardId = boardId, Name = "Bob" });

        var sut = fixture.ListClients;
        var result = await sut.HandleAsync("Ali", 0, 50, CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().ContainSingle(c => c.Name == "Alice");
    }

    [Fact]
    public async Task ListClients_WithPagination_SkipsAndTakes()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        for (var i = 0; i < 10; i++)
            StoreInDb(new Client { BoardId = boardId, Name = $"Client{i}" });

        var sut = fixture.ListClients;
        var result = await sut.HandleAsync(null, 2, 3, CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().HaveCount(3);
        result.Value[0].Name.Should().Be("Client2");
        result.Value[1].Name.Should().Be("Client3");
        result.Value[2].Name.Should().Be("Client4");
    }

    [Fact]
    public async Task ListClients_WithPagination_DefaultsToFirst50()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        for (var i = 0; i < 60; i++)
            StoreInDb(new Client { BoardId = boardId, Name = $"Client{i}" });

        var sut = fixture.ListClients;
        var result = await sut.HandleAsync(null, 0, 50, CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().HaveCount(50);
        result.Value[0].Name.Should().Be("Client0");
    }

    [Fact]
    public async Task GetClient_WithValidId_ReturnsClient()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        var client = new Client { BoardId = boardId, Name = "Alice", Phone = "123" };
        StoreInDb(client);

        var sut = fixture.GetClient;
        var result = await sut.HandleAsync(client.Id, CancellationToken.None);

        AssertSuccess(result);
        result.Value!.Name.Should().Be("Alice");
        result.Value.Phone.Should().Be("123");
    }

    [Fact]
    public async Task GetClient_WithInvalidId_ReturnsFailure()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.GetClient;
        var result = await sut.HandleAsync(999L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateClient_WithValidData_UpdatesName()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        var client = new Client { BoardId = boardId, Name = "Original" };
        StoreInDb(client);

        var sut = fixture.UpdateClient;
        var result = await sut.HandleAsync(client.Id, new UpdateClientRequest("Updated", null, null),
            CancellationToken.None);

        AssertSuccess(result);
        var updated = Db.Find<Client>(client.Id);
        updated!.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task DeleteClient_WithNoAppointments_Deletes()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        var client = new Client { BoardId = boardId, Name = "Alice" };
        StoreInDb(client);

        var sut = fixture.DeleteClient;
        var result = await sut.HandleAsync(client.Id, CancellationToken.None);

        AssertSuccess(result);
        var deleted = Db.Find<Client>(client.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteClient_WithExistingAppointments_ReturnsFailure()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        var client = new Client { BoardId = boardId, Name = "Alice" };
        var service = new Service
            { Name = "Cut", BoardId = boardId, Category = ServiceCategory.MensHaircut, BaseDuration = 30 };
        StoreInDb(client);
        StoreInDb(service);
        var appointment = new Appointment
        {
            BoardId = boardId,
            ClientId = client.Id,
            ServiceId = service.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
        };
        StoreInDb(appointment);

        var sut = fixture.DeleteClient;
        var result = await sut.HandleAsync(client.Id, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        AssertEntityExists<Client>((SchedulerDbContext)DbContext, client.Id);
    }

    [Fact]
    public async Task CreateClient_WithInvalidBoard_ReturnsNotFoundErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.CreateClient;
        var request = new CreateClientRequest(999L, "John", null, null);

        var result = await sut.HandleAsync(request, CancellationToken.None);

        AssertNotFound(result);
        AssertNoEntityInDb<Client>((SchedulerDbContext)DbContext);
    }

    [Fact]
    public async Task CreateClient_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId, memberUserIds: [2L]);
        var sut = fixture.CreateClient;
        var request = new CreateClientRequest(boardId, "John", null, null);

        var result = await sut.HandleAsync(request, CancellationToken.None);

        AssertAccessDenied(result);
        AssertNoEntityInDb<Client>((SchedulerDbContext)DbContext);
    }

    [Fact]
    public async Task GetClient_WithInvalidId_ReturnsNotFoundErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.GetClient;
        var result = await sut.HandleAsync(999L, CancellationToken.None);

        AssertNotFound(result);
    }

    [Fact]
    public async Task GetClient_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var otherBoard = new Board { Name = "Other", MemberUserIds = [2L] };
        StoreInDb(otherBoard);
        var otherClient = new Client { BoardId = otherBoard.Id, Name = "Bob" };
        StoreInDb(otherClient);

        var sut = fixture.GetClient;
        var result = await sut.HandleAsync(otherClient.Id, CancellationToken.None);

        AssertAccessDenied(result);
    }

    [Fact]
    public async Task UpdateClient_WithInvalidId_ReturnsNotFoundErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.UpdateClient;
        var result = await sut.HandleAsync(999L, new UpdateClientRequest("Updated", null, null),
            CancellationToken.None);

        AssertNotFound(result);
    }

    [Fact]
    public async Task UpdateClient_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId, memberUserIds: [2L]);
        var client = new Client { BoardId = boardId, Name = "Bob" };
        StoreInDb(client);

        var sut = fixture.UpdateClient;
        var result = await sut.HandleAsync(client.Id, new UpdateClientRequest("Updated", null, null),
            CancellationToken.None);

        AssertAccessDenied(result);
        var reloaded = Db.FindFresh<Client>(client.Id);
        reloaded!.Name.Should().Be("Bob");
    }

    [Fact]
    public async Task DeleteClient_WithInvalidId_ReturnsNotFoundErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.DeleteClient;
        var result = await sut.HandleAsync(999L, CancellationToken.None);

        AssertNotFound(result);
    }

    [Fact]
    public async Task DeleteClient_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId, memberUserIds: [2L]);
        var client = new Client { BoardId = boardId, Name = "Bob" };
        StoreInDb(client);

        var sut = fixture.DeleteClient;
        var result = await sut.HandleAsync(client.Id, CancellationToken.None);

        AssertAccessDenied(result);
        AssertEntityExists<Client>((SchedulerDbContext)DbContext, client.Id);
    }

    [Fact]
    public async Task DeleteClient_WithExistingAppointments_ReturnsConflictErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        var client = new Client { BoardId = boardId, Name = "Alice" };
        var service = new Service
            { Name = "Cut", BoardId = boardId, Category = ServiceCategory.MensHaircut, BaseDuration = 30 };
        StoreInDb(client);
        StoreInDb(service);
        var appointment = new Appointment
        {
            BoardId = boardId,
            ClientId = client.Id,
            ServiceId = service.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
        };
        StoreInDb(appointment);

        var sut = fixture.DeleteClient;
        var result = await sut.HandleAsync(client.Id, CancellationToken.None);

        AssertConflict(result);
        AssertEntityExists<Client>((SchedulerDbContext)DbContext, client.Id);
    }
}
