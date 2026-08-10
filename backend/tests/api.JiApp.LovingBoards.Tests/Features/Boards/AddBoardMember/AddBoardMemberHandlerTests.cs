using JiApp.Common.Authentication;
using api.JiApp.LovingBoards.Clients;
using api.JiApp.LovingBoards.Configuration;
using api.JiApp.LovingBoards.Features.Boards.AddBoardMember;
using api.JiApp.LovingBoards.Realtime;
using api.JiApp.LovingBoards.Tests.Realtime;

namespace api.JiApp.LovingBoards.Tests.Features.Boards.AddBoardMember;

public sealed class AddBoardMemberHandlerTests : HandlerTestBase<LovingBoardsDbContext>
{
    private static readonly LovingBoardsSettings DefaultSettings = new()
    {
        ConnectionString = "Data Source=:memory:",
        Jwt = new JwtSettings { Key = "key", Issuer = "iss", Audience = "aud" },
        MaxMembersPerBoard = 2,
        DefaultPageSize = 50,
        MaxBoardNameLength = 200
    };

    private sealed class Fixture
    {
        private readonly ILovingBoardsDbContext _dbContext;
        private readonly TestDb _testDb;
        private readonly ICurrentUserService _currentUser;
        private readonly IBoardBroadcaster _broadcaster = new NoOpBoardBroadcaster();
        private readonly UserExistenceClientDouble _userExistenceClient = UserExistenceClientDouble.Found();

        private Fixture(ILovingBoardsDbContext dbContext, TestDb testDb)
        {
            _dbContext = dbContext;
            _testDb = testDb;
            _currentUser = MockCurrentUserService.GetSuccessful().Mock.Object;
        }

        public AddBoardMemberHandler Sut =>
            new(_dbContext, _currentUser, _broadcaster, new BoardWriteLock(), DefaultSettings, _userExistenceClient.Object);

        public static Fixture Init(ILovingBoardsDbContext dbContext, TestDb testDb) => new(dbContext, testDb);

        public Fixture WithBoard(out long boardId, List<long>? memberUserIds = null)
        {
            var members = new List<long> { 1L };
            if (memberUserIds is not null)
                members.AddRange(memberUserIds.Where(id => id != 1L));
            var board = new Board { Name = "Test", OwnerUserId = 1L, MemberUserIds = members.Distinct().ToList() };
            _testDb.Store(board);
            boardId = board.Id;
            return this;
        }

        public Fixture WithUserMissing()
        {
            _userExistenceClient.WithStatus(UserExistenceStatus.NotFound);
            return this;
        }

        public Fixture WithIdentityUnavailable()
        {
            _userExistenceClient.WithStatus(UserExistenceStatus.Unavailable);
            return this;
        }
    }

    [Fact]
    public async Task AddBoardMember_UserExists_AddsMember()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);

        var result = await fixture.Sut.HandleAsync(boardId, new AddBoardMemberRequest(2L), CancellationToken.None);

        AssertSuccess(result);
        var updated = Db.Find<Board>(boardId);
        updated!.MemberUserIds.Should().Contain([1L, 2L]);
    }

    [Fact]
    public async Task AddBoardMember_CapReached_ReturnsValidationFailure()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId, memberUserIds: [1L, 2L]);

        var result = await fixture.Sut.HandleAsync(boardId, new AddBoardMemberRequest(3L), CancellationToken.None);

        AssertValidationFailure(result);
        result.Error.Should().Contain("Maximum number of members");
        var reloaded = Db.FindFresh<Board>(boardId);
        reloaded!.MemberUserIds.Should().Equal([1L, 2L]);
    }

    [Fact]
    public async Task AddBoardMember_DuplicateMember_ReturnsConflict()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);

        var result = await fixture.Sut.HandleAsync(boardId, new AddBoardMemberRequest(1L), CancellationToken.None);

        AssertConflict(result);
        result.Error.Should().Contain("already a member");
        var reloaded = Db.FindFresh<Board>(boardId);
        reloaded!.MemberUserIds.Should().Equal([1L]);
    }

    [Fact]
    public async Task AddBoardMember_AtCapButAlreadyMember_ReturnsConflict()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId, memberUserIds: [1L, 2L]);

        var result = await fixture.Sut.HandleAsync(boardId, new AddBoardMemberRequest(1L), CancellationToken.None);

        // Duplicate check wins over the cap check: re-adding an existing member is a Conflict, not a cap violation.
        AssertConflict(result);
        result.Error.Should().Contain("already a member");
        var reloaded = Db.FindFresh<Board>(boardId);
        reloaded!.MemberUserIds.Should().Equal([1L, 2L]);
    }

    [Fact]
    public async Task AddBoardMember_UserNotFound_ReturnsNotFound()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId).WithUserMissing();

        var result = await fixture.Sut.HandleAsync(boardId, new AddBoardMemberRequest(2L), CancellationToken.None);

        AssertNotFound(result);
        var reloaded = Db.FindFresh<Board>(boardId);
        reloaded!.MemberUserIds.Should().Equal([1L]);
    }

    [Fact]
    public async Task AddBoardMember_IdentityUnavailable_FailsClosed()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId).WithIdentityUnavailable();

        var result = await fixture.Sut.HandleAsync(boardId, new AddBoardMemberRequest(2L), CancellationToken.None);

        AssertFailure(result, ResultCategories.Unavailable);
        var reloaded = Db.FindFresh<Board>(boardId);
        reloaded!.MemberUserIds.Should().Equal([1L]);
    }
}
