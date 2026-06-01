using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Persistence;

namespace JiApp.Scheduler.Features.Services.GetService;

public sealed class GetServiceHandler(ISchedulerDbContext db, ICurrentUserService currentUser)
{
    public async Task<Result<ServiceResponse>> HandleAsync(long id, CancellationToken ct)
    {
        var service = await db.Services.FindAsync([id], ct);
        if (service is null)
            return Result<ServiceResponse>.Failure("Service not found", ResultCategories.NotFound);

        var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, service.BoardId, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<ServiceResponse>.Failure(boardResult.Error!, boardResult.ErrorCategory);

        var response = new ServiceResponse(
            service.Id,
            service.BoardId,
            service.Name,
            service.Category.ToString(),
            service.BaseDuration,
            new PriceResponse(service.BasePrice.Amount, service.BasePrice.Currency));

        return Result<ServiceResponse>.Success(response);
    }
}