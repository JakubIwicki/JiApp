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

public sealed class BoardHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SchedulerDbContext _db;
    private readonly Mock<ICurrentUserService> _currentUser;

    public BoardHandlerTests()
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

    [Fact]
    public async Task CreateBoard_WithValidName_ReturnsBoardId()
    {
        var handler = new CreateBoardHandler(_db, _currentUser.Object);
        var request = new CreateBoardRequest("My Board");

        var result = await handler.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateBoard_SetsCreatorAsMember()
    {
        var handler = new CreateBoardHandler(_db, _currentUser.Object);
        var request = new CreateBoardRequest("My Board");

        await handler.HandleAsync(request, CancellationToken.None);

        var board = await _db.Boards.FirstAsync();
        board.MemberUserIds.Should().Contain(1L);
    }

    [Fact]
    public async Task GetBoard_WithValidId_ReturnsBoard()
    {
        var board = new Board { Name = "Test", MemberUserIds = [1L] };
        _db.Boards.Add(board);
        await _db.SaveChangesAsync();

        var handler = new GetBoardHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(board.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Test");
        result.Value.MemberUserIds.Should().Contain(1L);
    }

    [Fact]
    public async Task GetBoard_WithInvalidId_ReturnsFailure()
    {
        var handler = new GetBoardHandler(_db, _currentUser.Object);

        var result = await handler.HandleAsync(999L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateBoard_WithValidId_UpdatesName()
    {
        var board = new Board { Name = "Original", MemberUserIds = [1L] };
        _db.Boards.Add(board);
        await _db.SaveChangesAsync();

        var handler = new UpdateBoardHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(board.Id, new UpdateBoardRequest("Updated"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var updated = await _db.Boards.FindAsync(board.Id);
        updated!.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task UpdateBoard_WithInvalidId_ReturnsFailure()
    {
        var handler = new UpdateBoardHandler(_db, _currentUser.Object);

        var result = await handler.HandleAsync(999L, new UpdateBoardRequest("Test"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task AddBoardMember_WithValidData_AddsMember()
    {
        var board = new Board { Name = "Test", MemberUserIds = [1L] };
        _db.Boards.Add(board);
        await _db.SaveChangesAsync();

        var handler = new AddBoardMemberHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(board.Id, new AddBoardMemberRequest(2L), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var updated = await _db.Boards.FindAsync(board.Id);
        updated!.MemberUserIds.Should().Contain([1L, 2L]);
    }

    [Fact]
    public async Task AddBoardMember_WithExistingMember_ReturnsFailure()
    {
        var board = new Board { Name = "Test", MemberUserIds = [1L] };
        _db.Boards.Add(board);
        await _db.SaveChangesAsync();

        var handler = new AddBoardMemberHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(board.Id, new AddBoardMemberRequest(1L), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteBoard_WithValidId_DeletesBoard()
    {
        var board = new Board { Name = "Test", MemberUserIds = [1L] };
        _db.Boards.Add(board);
        await _db.SaveChangesAsync();

        var handler = new DeleteBoardHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(board.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(board.Id);
        var deleted = await _db.Boards.FindAsync(board.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteBoard_WithInvalidId_ReturnsFailure()
    {
        var handler = new DeleteBoardHandler(_db, _currentUser.Object);

        var result = await handler.HandleAsync(999L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Board not found");
    }

    [Fact]
    public async Task DeleteBoard_ByNonMember_ReturnsAccessDenied()
    {
        var board = new Board { Name = "Test", MemberUserIds = [2L] };
        _db.Boards.Add(board);
        await _db.SaveChangesAsync();

        var handler = new DeleteBoardHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(board.Id, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Access denied");
    }

    [Fact]
    public async Task ListBoards_ReturnsOnlyUsersBoards()
    {
        var myBoard = new Board { Name = "Mine", MemberUserIds = [1L] };
        var otherBoard = new Board { Name = "Theirs", MemberUserIds = [2L] };
        var sharedBoard = new Board { Name = "Shared", MemberUserIds = [1L, 2L] };
        _db.Boards.AddRange(myBoard, otherBoard, sharedBoard);
        await _db.SaveChangesAsync();

        var handler = new ListBoardsHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Boards.Should().HaveCount(2);
        result.Value!.Boards.Select(b => b.Id).Should().Contain([myBoard.Id, sharedBoard.Id]);
        result.Value!.Boards.Select(b => b.Id).Should().NotContain(otherBoard.Id);
    }

    [Fact]
    public async Task ListBoards_ReturnsEmptyForUserWithNoBoards()
    {
        var otherBoard = new Board { Name = "Theirs", MemberUserIds = [2L] };
        _db.Boards.Add(otherBoard);
        await _db.SaveChangesAsync();

        var handler = new ListBoardsHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Boards.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveBoardMember_RemovesMember()
    {
        var board = new Board { Name = "Test", MemberUserIds = [1L, 2L] };
        _db.Boards.Add(board);
        await _db.SaveChangesAsync();

        var handler = new RemoveBoardMemberHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(board.Id, 2L, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var updated = await _db.Boards.FindAsync(board.Id);
        updated!.MemberUserIds.Should().ContainSingle().Which.Should().Be(1L);
    }

    [Fact]
    public async Task RemoveBoardMember_WithInvalidBoard_ReturnsFailure()
    {
        var handler = new RemoveBoardMemberHandler(_db, _currentUser.Object);

        var result = await handler.HandleAsync(999L, 2L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Board not found");
    }

    [Fact]
    public async Task RemoveBoardMember_ByNonMember_ReturnsAccessDenied()
    {
        var board = new Board { Name = "Test", MemberUserIds = [2L, 3L] };
        _db.Boards.Add(board);
        await _db.SaveChangesAsync();

        var handler = new RemoveBoardMemberHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(board.Id, 3L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Access denied");
    }

    [Fact]
    public async Task RemoveBoardMember_WithNonExistentMember_ReturnsFailure()
    {
        var board = new Board { Name = "Test", MemberUserIds = [1L, 2L] };
        _db.Boards.Add(board);
        await _db.SaveChangesAsync();

        var handler = new RemoveBoardMemberHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(board.Id, 999L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Member not found");
    }

    [Fact]
    public async Task RemoveBoardMember_WhenLastMember_ReturnsFailure()
    {
        var board = new Board { Name = "Test", MemberUserIds = [1L] };
        _db.Boards.Add(board);
        await _db.SaveChangesAsync();

        var handler = new RemoveBoardMemberHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(board.Id, 1L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Cannot remove the last member");
    }

    [Fact]
    public async Task DeleteBoard_WithInvalidId_ReturnsNotFoundErrorCategory()
    {
        var handler = new DeleteBoardHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(999L, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }

    [Fact]
    public async Task DeleteBoard_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        var board = new Board { Name = "Test", MemberUserIds = [2L] };
        _db.Boards.Add(board);
        await _db.SaveChangesAsync();

        var handler = new DeleteBoardHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(board.Id, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.AccessDenied);
    }

    [Fact]
    public async Task RemoveBoardMember_WithInvalidBoard_ReturnsNotFoundErrorCategory()
    {
        var handler = new RemoveBoardMemberHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(999L, 2L, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }

    [Fact]
    public async Task RemoveBoardMember_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        var board = new Board { Name = "Test", MemberUserIds = [2L, 3L] };
        _db.Boards.Add(board);
        await _db.SaveChangesAsync();

        var handler = new RemoveBoardMemberHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(board.Id, 3L, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.AccessDenied);
    }

    [Fact]
    public async Task RemoveBoardMember_WithNonExistentMember_ReturnsNotFoundErrorCategory()
    {
        var board = new Board { Name = "Test", MemberUserIds = [1L, 2L] };
        _db.Boards.Add(board);
        await _db.SaveChangesAsync();

        var handler = new RemoveBoardMemberHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(board.Id, 999L, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }

    [Fact]
    public async Task RemoveBoardMember_WhenLastMember_ReturnsConflictErrorCategory()
    {
        var board = new Board { Name = "Test", MemberUserIds = [1L] };
        _db.Boards.Add(board);
        await _db.SaveChangesAsync();

        var handler = new RemoveBoardMemberHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(board.Id, 1L, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.Conflict);
    }

    [Fact]
    public async Task GetBoard_WithInvalidId_ReturnsNotFoundErrorCategory()
    {
        var handler = new GetBoardHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(999L, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }

    [Fact]
    public async Task GetBoard_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        var board = new Board { Name = "Test", MemberUserIds = [2L] };
        _db.Boards.Add(board);
        await _db.SaveChangesAsync();

        var handler = new GetBoardHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(board.Id, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.AccessDenied);
    }

    [Fact]
    public async Task UpdateBoard_WithInvalidId_ReturnsNotFoundErrorCategory()
    {
        var handler = new UpdateBoardHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(999L, new UpdateBoardRequest("Test"), CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }

    [Fact]
    public async Task UpdateBoard_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        var board = new Board { Name = "Test", MemberUserIds = [2L] };
        _db.Boards.Add(board);
        await _db.SaveChangesAsync();

        var handler = new UpdateBoardHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(board.Id, new UpdateBoardRequest("Test"), CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.AccessDenied);
    }
}