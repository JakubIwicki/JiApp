namespace api.JiApp.LovingBoards.Features.Boards.GetBoard;

[Serializable]
public sealed record GetBoardResponse(long Id, string Name, long OwnerUserId, List<long> MemberUserIds, DateTime CreatedAt);
