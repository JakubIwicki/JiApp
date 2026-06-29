using JiApp.Common.Services;
using api.JiApp.LovingBoards.Features.Common;
using api.JiApp.LovingBoards.Tests.Bases;

namespace api.JiApp.LovingBoards.Tests.Features.Common;

public sealed class BoardAccessGuardTests : LovingBoardsHandlerTestBase
{
    private sealed class Fixture
    {
        private readonly ILovingBoardsDbContext _dbContext;
        private readonly LovingBoardsDbContext _db;
        private readonly ICurrentUserService _currentUser;

        private Fixture(ILovingBoardsDbContext dbContext, TestDb testDb)
        {
            _dbContext = dbContext;
            _db = (LovingBoardsDbContext)dbContext;
            _currentUser = MockCurrentUserService.GetSuccessful().Mock.Object;
        }

        public ICurrentUserService CurrentUser => _currentUser;
        public ILovingBoardsDbContext DbContext => _dbContext;

        public static Fixture Init(ILovingBoardsDbContext dbContext, TestDb testDb) => new(dbContext, testDb);

        public Fixture WithBoard(out Board board, List<long>? memberUserIds = null)
        {
            board = new Board { Name = "Test", OwnerUserId = 1L, MemberUserIds = memberUserIds ?? [1L] };
            _db.Boards.Add(board);
            _db.SaveChanges();
            _db.ChangeTracker.Clear();
            return this;
        }
    }

    [Fact]
    public async Task VerifyBoardAccess_WhenBoardExistsAndUserIsMember_ReturnsBoard()
    {
        var fixture = Fixture.Init(DbContext, Db);
        fixture.WithBoard(out var board);

        var result =
            await BoardAccessGuard.VerifyBoardAccessAsync(fixture.DbContext, board.Id, fixture.CurrentUser, CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(board.Id);
        result.Value.Name.Should().Be("Test");
    }

    [Fact]
    public async Task VerifyBoardAccess_WhenBoardDoesNotExist_ReturnsNotFound()
    {
        var fixture = Fixture.Init(DbContext, Db);

        var result =
            await BoardAccessGuard.VerifyBoardAccessAsync(fixture.DbContext, 999L, fixture.CurrentUser, CancellationToken.None);

        AssertNotFound(result);
        result.Error.Should().Be("Board not found");
    }

    [Fact]
    public async Task VerifyBoardAccess_WhenUserIsNotMember_ReturnsAccessDenied()
    {
        var fixture = Fixture.Init(DbContext, Db);
        fixture.WithBoard(out var board, [2L]);

        var result =
            await BoardAccessGuard.VerifyBoardAccessAsync(fixture.DbContext, board.Id, fixture.CurrentUser, CancellationToken.None);

        AssertAccessDenied(result);
        result.Error.Should().Be("Access denied");
    }
}
