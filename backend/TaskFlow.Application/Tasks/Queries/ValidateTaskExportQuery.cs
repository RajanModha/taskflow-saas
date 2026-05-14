using MediatR;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Application.Tasks.Queries;

public sealed record ValidateTaskExportQuery(TaskExportFilters Filters)
    : IRequest<ValidateTaskExportResult>;

public abstract record ValidateTaskExportResult;

public sealed record TaskExportValidated(IReadOnlyDictionary<Guid, string> AssigneeDisplayNames)
    : ValidateTaskExportResult;

public sealed record TaskExportTooManyRows : ValidateTaskExportResult;
