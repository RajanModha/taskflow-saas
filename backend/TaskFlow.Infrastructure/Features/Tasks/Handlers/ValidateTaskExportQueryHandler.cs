using MediatR;
using TaskFlow.Application.Tasks.Queries;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class ValidateTaskExportQueryHandler(ITaskExportRepository taskExportRepository)
    : IRequestHandler<ValidateTaskExportQuery, ValidateTaskExportResult>
{
    private const int MaxExportRows = 10_000;

    public async Task<ValidateTaskExportResult> Handle(
        ValidateTaskExportQuery request,
        CancellationToken cancellationToken)
    {
        var total = await taskExportRepository.GetExportCountAsync(request.Filters, cancellationToken);
        if (total > MaxExportRows)
        {
            return new TaskExportTooManyRows();
        }

        var assigneeNames = await taskExportRepository.GetExportAssigneeDisplayNamesAsync(
            request.Filters,
            cancellationToken);
        return new TaskExportValidated(assigneeNames);
    }
}
