using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Domain;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Scheduler.Features.Services.ListServices;

public sealed class ListServicesHandler(ISchedulerDbContext db, ICurrentUserService currentUser)
{
    public async Task<Result<List<ServiceResponse>>> HandleAsync(long boardId, string? category, CancellationToken ct)
    {
        var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, boardId, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<List<ServiceResponse>>.Failure(boardResult.Error!, boardResult.ErrorCategory);

        var query = db.Services.AsQueryable();
        query = query.Where(s => s.BoardId == boardId);

        if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<ServiceCategory>(category, true, out var parsed))
            query = query.Where(s => s.Category == parsed);

        var services = await query
            .OrderBy(s => s.Name)
            .Select(s => new ServiceResponse(
                s.Id,
                s.BoardId,
                s.Name,
                s.Category.ToString(),
                s.BaseDuration,
                new PriceResponse(s.BasePrice.Amount, s.BasePrice.Currency)))
            .ToListAsync(ct);

        return Result<List<ServiceResponse>>.Success(services);
    }
}