using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Boards.AddBoardMember;
using JiApp.Scheduler.Features.Boards.CreateBoard;
using JiApp.Scheduler.Features.Boards.DeleteBoard;
using JiApp.Scheduler.Features.Boards.GetBoard;
using JiApp.Scheduler.Features.Boards.ListBoards;
using JiApp.Scheduler.Features.Boards.RemoveBoardMember;
using JiApp.Scheduler.Features.Boards.UpdateBoard;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace JiApp.Scheduler.Tests.Features.Boards;

public sealed class BoardHandlerTests
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

        public Fixture WithBoard(string name = "Test", List<long>? memberUserIds = null)
        {
            var board = new Board { Name = name, MemberUserIds = memberUserIds ?? [1L] };
            _db.Boards.Add(board);
            _db.SaveChanges();
            _db.ChangeTracker.Clear();
            return this;
        }

        // Overload returning the board ID for tests that need it
        public Fixture WithBoard(out long boardId, string name = "Test", List<long>? memberUserIds = null)
        {
            var board = new Board { Name = name, MemberUserIds = memberUserIds ?? [1L] };
            _db.Boards.Add(board);
            _db.SaveChanges();
            boardId = board.Id;
            _db.ChangeTracker.Clear();
            return this;
        }

        public CreateBoardHandler CreateBoardSut => new(_db, _currentUser.Object);
        public GetBoardHandler GetBoardSut => new(_db, _currentUser.Object);
        public UpdateBoardHandler UpdateBoardSut => new(_db, _currentUser.Object);
        public DeleteBoardHandler DeleteBoardSut => new(_db, _currentUser.Object);
        public ListBoardsHandler ListBoardsSut => new(_db, _currentUser.Object);
        public AddBoardMemberHandler AddBoardMemberSut => new(_db, _currentUser.Object);
        public RemoveBoardMemberHandler RemoveBoardMemberSut => new(_db, _currentUser.Object);

        public void Dispose()
        {
            _db.Dispose();
            _connection.Close();
        }
    }

    [Fact]
    public async Task CreateBoard_WithValidName_ReturnsBoardId()
    {
        using var fixture = new Fixture();
        var sut = fixture.CreateBoardSut;
        var request = new CreateBoardRequest("My Board");

        var result = await sut.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateBoard_SetsCreatorAsMember()
    {
        using var fixture = new Fixture();
        var sut = fixture.CreateBoardSut;
        var request = new CreateBoardRequest("My Board");

        await sut.HandleAsync(request, CancellationToken.None);

        var board = await fixture.Db.Boards.FirstAsync();
        board.MemberUserIds.Should().Contain(1L);
    }

    [Fact]
    public async Task GetBoard_WithValidId_ReturnsBoard()
    {
        using var fixture = new Fixture().WithBoard(out var boardId);
        var sut = fixture.GetBoardSut;

        var result = await sut.HandleAsync(boardId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Test");
        result.Value.MemberUserIds.Should().Contain(1L);
    }

    [Fact]
    public async Task GetBoard_WithInvalidId_ReturnsFailure()
    {
        using var fixture = new Fixture();
        var sut = fixture.GetBoardSut;

        var result = await sut.HandleAsync(999L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateBoard_WithValidId_UpdatesName()
    {
        using var fixture = new Fixture().WithBoard(out var boardId);
        var sut = fixture.UpdateBoardSut;

        var result = await sut.HandleAsync(boardId, new UpdateBoardRequest("Updated"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var updated = await fixture.Db.Boards.FindAsync(boardId);
        updated!.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task UpdateBoard_WithInvalidId_ReturnsFailure()
    {
        using var fixture = new Fixture();
        var sut = fixture.UpdateBoardSut;

        var result = await sut.HandleAsync(999L, new UpdateBoardRequest("Test"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task AddBoardMember_WithValidData_AddsMember()
    {
        using var fixture = new Fixture().WithBoard(out var boardId);
        var sut = fixture.AddBoardMemberSut;

        var result = await sut.HandleAsync(boardId, new AddBoardMemberRequest(2L), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var updated = await fixture.Db.Boards.FindAsync(boardId);
        updated!.MemberUserIds.Should().Contain([1L, 2L]);
    }

    [Fact]
    public async Task AddBoardMember_WithExistingMember_ReturnsFailure()
    {
        using var fixture = new Fixture().WithBoard(out var boardId);
        var sut = fixture.AddBoardMemberSut;

        var result = await sut.HandleAsync(boardId, new AddBoardMemberRequest(1L), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteBoard_WithValidId_DeletesBoard()
    {
        using var fixture = new Fixture().WithBoard(out var boardId);
        var sut = fixture.DeleteBoardSut;

        var result = await sut.HandleAsync(boardId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(boardId);
        var deleted = await fixture.Db.Boards.FindAsync(boardId);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteBoard_WithInvalidId_ReturnsFailure()
    {
        using var fixture = new Fixture();
        var sut = fixture.DeleteBoardSut;

        var result = await sut.HandleAsync(999L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Board not found");
    }

    [Fact]
    public async Task DeleteBoard_ByNonMember_ReturnsAccessDenied()
    {
        using var fixture = new Fixture().WithBoard(out var boardId, memberUserIds: [2L]);
        var sut = fixture.DeleteBoardSut;

        var result = await sut.HandleAsync(boardId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Access denied");
    }

    [Fact]
    public async Task ListBoards_ReturnsOnlyUsersBoards()
    {
        using var fixture = new Fixture()
            .WithBoard(out var myBoardId, name: "Mine")
            .WithBoard(out var sharedBoardId, name: "Shared", memberUserIds: [1L, 2L]);
        var otherBoard = new Board { Name = "Theirs", MemberUserIds = [2L] };
        fixture.Db.Boards.Add(otherBoard);
        await fixture.Db.SaveChangesAsync();
        var sut = fixture.ListBoardsSut;

        var result = await sut.HandleAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Boards.Should().HaveCount(2);
        result.Value!.Boards.Select(b => b.Id).Should().Contain([myBoardId, sharedBoardId]);
        result.Value!.Boards.Select(b => b.Id).Should().NotContain(otherBoard.Id);
    }

    [Fact]
    public async Task ListBoards_ReturnsEmptyForUserWithNoBoards()
    {
        using var fixture = new Fixture();
        var otherBoard = new Board { Name = "Theirs", MemberUserIds = [2L] };
        fixture.Db.Boards.Add(otherBoard);
        await fixture.Db.SaveChangesAsync();
        var sut = fixture.ListBoardsSut;

        var result = await sut.HandleAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Boards.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveBoardMember_RemovesMember()
    {
        using var fixture = new Fixture().WithBoard(out var boardId, memberUserIds: [1L, 2L]);
        var sut = fixture.RemoveBoardMemberSut;

        var result = await sut.HandleAsync(boardId, 2L, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var updated = await fixture.Db.Boards.FindAsync(boardId);
        updated!.MemberUserIds.Should().ContainSingle().Which.Should().Be(1L);
    }

    [Fact]
    public async Task RemoveBoardMember_WithInvalidBoard_ReturnsFailure()
    {
        using var fixture = new Fixture();
        var sut = fixture.RemoveBoardMemberSut;

        var result = await sut.HandleAsync(999L, 2L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Board not found");
    }

    [Fact]
    public async Task RemoveBoardMember_ByNonMember_ReturnsAccessDenied()
    {
        using var fixture = new Fixture().WithBoard(out var boardId, memberUserIds: [2L, 3L]);
        var sut = fixture.RemoveBoardMemberSut;

        var result = await sut.HandleAsync(boardId, 3L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Access denied");
    }

    [Fact]
    public async Task RemoveBoardMember_WithNonExistentMember_ReturnsFailure()
    {
        using var fixture = new Fixture().WithBoard(out var boardId, memberUserIds: [1L, 2L]);
        var sut = fixture.RemoveBoardMemberSut;

        var result = await sut.HandleAsync(boardId, 999L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Member not found");
    }

    [Fact]
    public async Task RemoveBoardMember_WhenLastMember_ReturnsFailure()
    {
        using var fixture = new Fixture().WithBoard(out var boardId);
        var sut = fixture.RemoveBoardMemberSut;

        var result = await sut.HandleAsync(boardId, 1L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Cannot remove the last member");
    }

    [Fact]
    public async Task DeleteBoard_WithInvalidId_ReturnsNotFoundErrorCategory()
    {
        using var fixture = new Fixture();
        var sut = fixture.DeleteBoardSut;

        var result = await sut.HandleAsync(999L, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }

    [Fact]
    public async Task DeleteBoard_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        using var fixture = new Fixture().WithBoard(out var boardId, memberUserIds: [2L]);
        var sut = fixture.DeleteBoardSut;

        var result = await sut.HandleAsync(boardId, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.AccessDenied);
    }

    [Fact]
    public async Task RemoveBoardMember_WithInvalidBoard_ReturnsNotFoundErrorCategory()
    {
        using var fixture = new Fixture();
        var sut = fixture.RemoveBoardMemberSut;

        var result = await sut.HandleAsync(999L, 2L, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }

    [Fact]
    public async Task RemoveBoardMember_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        using var fixture = new Fixture().WithBoard(out var boardId, memberUserIds: [2L, 3L]);
        var sut = fixture.RemoveBoardMemberSut;

        var result = await sut.HandleAsync(boardId, 3L, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.AccessDenied);
    }

    [Fact]
    public async Task RemoveBoardMember_WithNonExistentMember_ReturnsNotFoundErrorCategory()
    {
        using var fixture = new Fixture().WithBoard(out var boardId, memberUserIds: [1L, 2L]);
        var sut = fixture.RemoveBoardMemberSut;

        var result = await sut.HandleAsync(boardId, 999L, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }

    [Fact]
    public async Task RemoveBoardMember_WhenLastMember_ReturnsConflictErrorCategory()
    {
        using var fixture = new Fixture().WithBoard(out var boardId);
        var sut = fixture.RemoveBoardMemberSut;

        var result = await sut.HandleAsync(boardId, 1L, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.Conflict);
    }

    [Fact]
    public async Task GetBoard_WithInvalidId_ReturnsNotFoundErrorCategory()
    {
        using var fixture = new Fixture();
        var sut = fixture.GetBoardSut;

        var result = await sut.HandleAsync(999L, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }

    [Fact]
    public async Task GetBoard_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        using var fixture = new Fixture().WithBoard(out var boardId, memberUserIds: [2L]);
        var sut = fixture.GetBoardSut;

        var result = await sut.HandleAsync(boardId, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.AccessDenied);
    }

    [Fact]
    public async Task UpdateBoard_WithInvalidId_ReturnsNotFoundErrorCategory()
    {
        using var fixture = new Fixture();
        var sut = fixture.UpdateBoardSut;

        var result = await sut.HandleAsync(999L, new UpdateBoardRequest("Test"), CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }

    [Fact]
    public async Task UpdateBoard_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        using var fixture = new Fixture().WithBoard(out var boardId, memberUserIds: [2L]);
        var sut = fixture.UpdateBoardSut;

        var result = await sut.HandleAsync(boardId, new UpdateBoardRequest("Test"), CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.AccessDenied);
    }
}
