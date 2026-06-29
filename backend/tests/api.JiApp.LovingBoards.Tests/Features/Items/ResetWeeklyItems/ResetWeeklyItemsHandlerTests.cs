using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using api.JiApp.LovingBoards.Domain;
using api.JiApp.LovingBoards.Features.Items.ResetWeeklyItems;
using api.JiApp.LovingBoards.Realtime;
using api.JiApp.LovingBoards.Tests.Bases;
using api.JiApp.LovingBoards.Tests.Realtime;

namespace api.JiApp.LovingBoards.Tests.Features.Items.ResetWeeklyItems;

public sealed class ResetWeeklyItemsHandlerTests : LovingBoardsHandlerTestBase
{
    private sealed class Fixture
    {
        private readonly ILovingBoardsDbContext _dbContext;
        private readonly TestDb _testDb;
        private readonly ICurrentUserService _currentUser;
        private readonly IBoardBroadcaster _broadcaster;

        private Fixture(ILovingBoardsDbContext dbContext, TestDb testDb)
        {
            _dbContext = dbContext;
            _testDb = testDb;
            _currentUser = MockCurrentUserService.GetSuccessful().Mock.Object;
            _broadcaster = new NoOpBoardBroadcaster();
        }

        public ResetWeeklyItemsHandler Sut => new(_dbContext, _currentUser, _broadcaster);

        public static Fixture Init(ILovingBoardsDbContext dbContext, TestDb testDb) => new(dbContext, testDb);

        public Fixture WithBoard(out long boardId, List<long>? memberUserIds = null)
        {
            var board = new Board { Name = "Test", OwnerUserId = 1L, MemberUserIds = memberUserIds ?? [1L] };
            _testDb.Store(board);
            boardId = board.Id;
            return this;
        }
    }

    [Fact]
    public async Task ForceReset_FlipsRecurringCompletedAndRemovedToNeeded()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        StoreInDb(new BoardItem { BoardId = boardId, Title = "Recurring Completed", IsRecurring = true, Status = BoardItemStatus.Completed, CompletedByUserId = 2L, AddedByUserId = 1L });
        StoreInDb(new BoardItem { BoardId = boardId, Title = "Recurring Removed", IsRecurring = true, Status = BoardItemStatus.Removed, RemovedAt = DateTime.UtcNow.AddHours(-1), AddedByUserId = 1L });
        StoreInDb(new BoardItem { BoardId = boardId, Title = "NonRecurring Completed", IsRecurring = false, Status = BoardItemStatus.Completed, CompletedByUserId = 2L, AddedByUserId = 1L });
        StoreInDb(new BoardItem { BoardId = boardId, Title = "Recurring Needed", IsRecurring = true, Status = BoardItemStatus.Needed, AddedByUserId = 1L });
        var sut = fixture.Sut;

        var result = await sut.HandleAsync(boardId, CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().Be(2);

        var items = await Db.Query<BoardItem>().ToListAsync();
        var recurringCompleted = items.Single(i => i.Title == "Recurring Completed");
        recurringCompleted.Status.Should().Be(BoardItemStatus.Needed);
        recurringCompleted.CompletedByUserId.Should().BeNull();

        var recurringRemoved = items.Single(i => i.Title == "Recurring Removed");
        recurringRemoved.Status.Should().Be(BoardItemStatus.Needed);
        recurringRemoved.RemovedAt.Should().BeNull();

        var nonRecurring = items.Single(i => i.Title == "NonRecurring Completed");
        nonRecurring.Status.Should().Be(BoardItemStatus.Completed);

        var recurringNeeded = items.Single(i => i.Title == "Recurring Needed");
        recurringNeeded.Status.Should().Be(BoardItemStatus.Needed);
    }

    [Fact]
    public async Task ForceReset_UpdatesLastWeeklyResetAt()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        StoreInDb(new BoardItem { BoardId = boardId, Title = "Item", IsRecurring = true, Status = BoardItemStatus.Completed, AddedByUserId = 1L });
        var sut = fixture.Sut;
        var before = DateTime.UtcNow;

        await sut.HandleAsync(boardId, CancellationToken.None);

        var board = Db.Find<Board>(boardId);
        board!.LastWeeklyResetAt.Should().NotBeNull();
        board.LastWeeklyResetAt!.Value.Should().BeOnOrAfter(before);
    }

    [Fact]
    public async Task ForceReset_ForcesEvenWhenWithinSameWeek()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        // Set LastWeeklyResetAt to now so it's within the current week
        var board = Db.Find<Board>(boardId);
        board!.LastWeeklyResetAt = DateTime.UtcNow;
        await DbContext.SaveChangesAsync();
        StoreInDb(new BoardItem { BoardId = boardId, Title = "Recurring Completed", IsRecurring = true, Status = BoardItemStatus.Completed, AddedByUserId = 1L });
        var sut = fixture.Sut;

        var result = await sut.HandleAsync(boardId, CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().Be(1);
        var item = await Db.Query<BoardItem>().FirstAsync();
        item.Status.Should().Be(BoardItemStatus.Needed);
    }

    [Fact]
    public async Task ForceReset_ReturnsZeroWhenNoRecurringItemsToFlip()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        StoreInDb(new BoardItem { BoardId = boardId, Title = "NonRecurring", IsRecurring = false, Status = BoardItemStatus.Completed, AddedByUserId = 1L });
        StoreInDb(new BoardItem { BoardId = boardId, Title = "Needed", IsRecurring = true, Status = BoardItemStatus.Needed, AddedByUserId = 1L });
        var sut = fixture.Sut;

        var result = await sut.HandleAsync(boardId, CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().Be(0);
    }

    [Fact]
    public async Task ForceReset_AccessDeniedForNonMember()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId, memberUserIds: [2L]);
        var sut = fixture.Sut;

        var result = await sut.HandleAsync(boardId, CancellationToken.None);

        AssertAccessDenied(result);
    }

    [Fact]
    public async Task ForceReset_NotFoundForMissingBoard()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.Sut;

        var result = await sut.HandleAsync(999L, CancellationToken.None);

        AssertNotFound(result);
    }

    // Publish events

    [Fact]
    public async Task ForceReset_PublishesRecurringReset()
    {
        var capturing = new CapturingBoardBroadcaster();
        var handler = new ResetWeeklyItemsHandler(DbContext, MockCurrentUserService.GetSuccessful().Mock.Object, capturing);
        var board = new Board { Name = "Test", OwnerUserId = 1L, MemberUserIds = [1L] };
        StoreInDb(board);
        StoreInDb(new BoardItem { BoardId = board.Id, Title = "Item", IsRecurring = true, Status = BoardItemStatus.Completed, AddedByUserId = 1L });

        await handler.HandleAsync(board.Id, CancellationToken.None);

        capturing.Published.Should().ContainSingle();
        capturing.Published[0].Ev.Event.Should().Be(BoardEventNames.RecurringReset);
    }
}
