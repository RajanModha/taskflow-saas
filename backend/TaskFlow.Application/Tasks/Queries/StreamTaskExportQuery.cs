using DomainTask = TaskFlow.Domain.Entities.Task;
using MediatR;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Application.Tasks.Queries;

public sealed record StreamTaskExportQuery(TaskExportFilters Filters)
    : IRequest<IAsyncEnumerable<DomainTask>>;
