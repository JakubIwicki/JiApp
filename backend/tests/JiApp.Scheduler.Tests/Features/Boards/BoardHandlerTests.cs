using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Boards.AddBoardMember;
using JiApp.Scheduler.Features.Boards.CreateBoard;
using JiApp.Scheduler.Features.Boards.DeleteBoard;
using JiApp.Scheduler.Features.Boards.GetBoard;
using JiApp.Scheduler.Features.Boards.ListBoards;
using JiApp.Scheduler.Features.Boards.RemoveBoardMember;
using JiApp.Scheduler.Features.Boards.UpdateBoard;

namespace JiApp.Scheduler.Tests.Features.Boards;

public sealed class BoardHandlerTests : HandlerTestBase
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

        public CreateBoardHandler Sut => new(_dbContext, _currentUser);
        public GetBoardHandler GetBoard => new(_dbContext, _currentUser);
        public UpdateBoardHandler UpdateBoard => new(_dbContext, _currentUser);
        public DeleteBoardHandler DeleteBoard => new(_dbContext, _currentUser);
        public ListBoardsHandler ListBoards => new(_dbContext, _currentUser);
        public AddBoardMemberHandler AddBoardMember => new(_dbContext, _currentUser);
        public RemoveBoardMemberHandler RemoveBoardMember => new(_dbContext, _currentUser);

        public static Fixture Init(ISchedulerDbContext dbContext, TestDb testDb) => new(dbContext, testDb);

        public Fixture WithBoard(string name = "Test", List<long>? memberUserIds = null, long ownerUserId = 1L)
        {
            var board = new Board { Name = name, OwnerUserId = ownerUserId, MemberUserIds = memberUserIds ?? [1L] };
            _testDb.Store(board);
            return this;
        }

        public Fixture WithBoard(out long boardId, string name = "Test", List<long>? memberUserIds = null, long ownerUserId = 1L)
        {
            var board = new Board { Name = name, OwnerUserId = ownerUserId, MemberUserIds = memberUserIds ?? [1L] };
            _testDb.Store(board);
            boardId = board.Id;
            return this;
        }
    }

    [Fact]
    public async Task CreateBoard_WithValidName_ReturnsBoardId()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.Sut;
        var request = new CreateBoardRequest("My Board");

        var result = await sut.HandleAsync(request, CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateBoard_SetsCreatorAsMember()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.Sut;
        var request = new CreateBoardRequest("My Board");

        await sut.HandleAsync(request, CancellationToken.None);

        var board = await Db.Query<Board>().FirstAsync();
        board.MemberUserIds.Should().Contain(1L);
    }

    [Fact]
    public async Task GetBoard_WithValidId_ReturnsBoard()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        var sut = fixture.GetBoard;

        var result = await sut.HandleAsync(boardId, CancellationToken.None);

        AssertSuccess(result);
        result.Value!.Name.Should().Be("Test");
        result.Value.MemberUserIds.Should().Contain(1L);
    }

    [Fact]
    public async Task GetBoard_WithInvalidId_ReturnsFailure()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.GetBoard;

        var result = await sut.HandleAsync(999L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateBoard_WithValidId_UpdatesName()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        var sut = fixture.UpdateBoard;

        var result = await sut.HandleAsync(boardId, new UpdateBoardRequest("Updated"), CancellationToken.None);

        AssertSuccess(result);
        var updated = Db.Find<Board>(boardId);
        updated!.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task UpdateBoard_WithInvalidId_ReturnsFailure()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.UpdateBoard;

        var result = await sut.HandleAsync(999L, new UpdateBoardRequest("Test"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task AddBoardMember_WithValidData_AddsMember()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        var sut = fixture.AddBoardMember;

        var result = await sut.HandleAsync(boardId, new AddBoardMemberRequest(2L), CancellationToken.None);

        AssertSuccess(result);
        var updated = Db.Find<Board>(boardId);
        updated!.MemberUserIds.Should().Contain([1L, 2L]);
    }

    [Fact]
    public async Task AddBoardMember_WithExistingMember_ReturnsFailure()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        var sut = fixture.AddBoardMember;

        var result = await sut.HandleAsync(boardId, new AddBoardMemberRequest(1L), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteBoard_WithValidId_DeletesBoard()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        var sut = fixture.DeleteBoard;

        var result = await sut.HandleAsync(boardId, CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().Be(boardId);
        var deleted = Db.Find<Board>(boardId);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteBoard_WithInvalidId_ReturnsFailure()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.DeleteBoard;

        var result = await sut.HandleAsync(999L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Board not found");
    }

    [Fact]
    public async Task DeleteBoard_ByNonMember_ReturnsAccessDenied()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId, memberUserIds: [2L], ownerUserId: 2L);
        var sut = fixture.DeleteBoard;

        var result = await sut.HandleAsync(boardId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Access denied");
    }

    [Fact]
    public async Task ListBoards_ReturnsOnlyUsersBoards()
    {
        var fixture = Fixture.Init(DbContext, Db)
            .WithBoard(out var myBoardId, name: "Mine")
            .WithBoard(out var sharedBoardId, name: "Shared", memberUserIds: [1L, 2L]);
        var otherBoard = new Board { Name = "Theirs", MemberUserIds = [2L] };
        StoreInDb(otherBoard);
        var sut = fixture.ListBoards;

        var result = await sut.HandleAsync(CancellationToken.None);

        AssertSuccess(result);
        result.Value!.Boards.Should().HaveCount(2);
        result.Value!.Boards.Select(b => b.Id).Should().Contain([myBoardId, sharedBoardId]);
        result.Value!.Boards.Select(b => b.Id).Should().NotContain(otherBoard.Id);
    }

    [Fact]
    public async Task ListBoards_ReturnsEmptyForUserWithNoBoards()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var otherBoard = new Board { Name = "Theirs", MemberUserIds = [2L] };
        StoreInDb(otherBoard);
        var sut = fixture.ListBoards;

        var result = await sut.HandleAsync(CancellationToken.None);

        AssertSuccess(result);
        result.Value!.Boards.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveBoardMember_RemovesMember()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId, memberUserIds: [1L, 2L]);
        var sut = fixture.RemoveBoardMember;

        var result = await sut.HandleAsync(boardId, 2L, CancellationToken.None);

        AssertSuccess(result);
        var updated = Db.Find<Board>(boardId);
        updated!.MemberUserIds.Should().ContainSingle().Which.Should().Be(1L);
    }

    [Fact]
    public async Task RemoveBoardMember_WithInvalidBoard_ReturnsFailure()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.RemoveBoardMember;

        var result = await sut.HandleAsync(999L, 2L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Board not found");
    }

    [Fact]
    public async Task RemoveBoardMember_ByNonMember_ReturnsAccessDenied()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId, memberUserIds: [2L, 3L], ownerUserId: 2L);
        var sut = fixture.RemoveBoardMember;

        var result = await sut.HandleAsync(boardId, 3L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Access denied");
    }

    [Fact]
    public async Task RemoveBoardMember_WithNonExistentMember_ReturnsFailure()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId, memberUserIds: [1L, 2L]);
        var sut = fixture.RemoveBoardMember;

        var result = await sut.HandleAsync(boardId, 999L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Member not found");
    }

    [Fact]
    public async Task RemoveBoardMember_WhenLastMember_ReturnsFailure()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        var sut = fixture.RemoveBoardMember;

        var result = await sut.HandleAsync(boardId, 1L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Cannot remove the last member");
    }

    [Fact]
    public async Task DeleteBoard_WithInvalidId_ReturnsNotFoundErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.DeleteBoard;

        var result = await sut.HandleAsync(999L, CancellationToken.None);

        AssertNotFound(result);
    }

    [Fact]
    public async Task DeleteBoard_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId, memberUserIds: [2L], ownerUserId: 2L);
        var sut = fixture.DeleteBoard;

        var result = await sut.HandleAsync(boardId, CancellationToken.None);

        AssertAccessDenied(result);
    }

    [Fact]
    public async Task RemoveBoardMember_WithInvalidBoard_ReturnsNotFoundErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.RemoveBoardMember;

        var result = await sut.HandleAsync(999L, 2L, CancellationToken.None);

        AssertNotFound(result);
    }

    [Fact]
    public async Task RemoveBoardMember_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId, memberUserIds: [2L, 3L], ownerUserId: 2L);
        var sut = fixture.RemoveBoardMember;

        var result = await sut.HandleAsync(boardId, 3L, CancellationToken.None);

        AssertAccessDenied(result);
    }

    [Fact]
    public async Task RemoveBoardMember_WithNonExistentMember_ReturnsNotFoundErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId, memberUserIds: [1L, 2L]);
        var sut = fixture.RemoveBoardMember;

        var result = await sut.HandleAsync(boardId, 999L, CancellationToken.None);

        AssertNotFound(result);
    }

    [Fact]
    public async Task RemoveBoardMember_WhenLastMember_ReturnsConflictErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        var sut = fixture.RemoveBoardMember;

        var result = await sut.HandleAsync(boardId, 1L, CancellationToken.None);

        AssertConflict(result);
    }

    [Fact]
    public async Task GetBoard_WithInvalidId_ReturnsNotFoundErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.GetBoard;

        var result = await sut.HandleAsync(999L, CancellationToken.None);

        AssertNotFound(result);
    }

    [Fact]
    public async Task GetBoard_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId, memberUserIds: [2L]);
        var sut = fixture.GetBoard;

        var result = await sut.HandleAsync(boardId, CancellationToken.None);

        AssertAccessDenied(result);
    }

    [Fact]
    public async Task UpdateBoard_WithInvalidId_ReturnsNotFoundErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.UpdateBoard;

        var result = await sut.HandleAsync(999L, new UpdateBoardRequest("Test"), CancellationToken.None);

        AssertNotFound(result);
    }

    [Fact]
    public async Task UpdateBoard_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId, memberUserIds: [2L]);
        var sut = fixture.UpdateBoard;

        var result = await sut.HandleAsync(boardId, new UpdateBoardRequest("Test"), CancellationToken.None);

        AssertAccessDenied(result);
    }

    [Fact]
    public async Task CreateBoard_SetsOwnerUserId()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.Sut;
        var request = new CreateBoardRequest("My Board");

        await sut.HandleAsync(request, CancellationToken.None);

        var board = await Db.Query<Board>().FirstAsync();
        board.OwnerUserId.Should().Be(1L);
    }

    [Fact]
    public async Task DeleteBoard_ByNonOwnerMember_ReturnsAccessDenied()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId, memberUserIds: [1L, 2L], ownerUserId: 2L);
        var sut = fixture.DeleteBoard;

        var result = await sut.HandleAsync(boardId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Access denied");
    }

    [Fact]
    public async Task AddBoardMember_ByNonOwnerMember_ReturnsAccessDenied()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId, memberUserIds: [1L, 2L], ownerUserId: 2L);
        var sut = fixture.AddBoardMember;

        var result = await sut.HandleAsync(boardId, new AddBoardMemberRequest(3L), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Access denied");
    }

    [Fact]
    public async Task RemoveBoardMember_ByNonOwnerMember_ReturnsAccessDenied()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId, memberUserIds: [1L, 2L], ownerUserId: 2L);
        var sut = fixture.RemoveBoardMember;

        var result = await sut.HandleAsync(boardId, 2L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Access denied");
    }

    [Fact]
    public async Task AddBoardMember_PersistsAcrossChangeTrackerClear()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        var sut = fixture.AddBoardMember;

        var result = await sut.HandleAsync(boardId, new AddBoardMemberRequest(2L), CancellationToken.None);

        AssertSuccess(result);
        ((SchedulerDbContext)DbContext).ChangeTracker.Clear();
        var reloaded = await Db.Query<Board>().FirstAsync(b => b.Id == boardId);
        reloaded.MemberUserIds.Should().Contain([1L, 2L]);
    }

    [Fact]
    public async Task RemoveBoardMember_PersistsAcrossChangeTrackerClear()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId, memberUserIds: [1L, 2L]);
        var sut = fixture.RemoveBoardMember;

        var result = await sut.HandleAsync(boardId, 2L, CancellationToken.None);

        AssertSuccess(result);
        ((SchedulerDbContext)DbContext).ChangeTracker.Clear();
        var reloaded = await Db.Query<Board>().FirstAsync(b => b.Id == boardId);
        reloaded.MemberUserIds.Should().ContainSingle().Which.Should().Be(1L);
    }
}
