using DomainTask = TaskFlow.Domain.Entities.Task;
using MediatR;
using TaskFlow.Application.Tasks.Queries;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class StreamTaskExportQueryHandler(ITaskExportRepository taskExportRepository)
    : IRequestHandler<StreamTaskExportQuery, IAsyncEnumerable<DomainTask>>
{
    public Task<IAsyncEnumerable<DomainTask>> Handle(
        StreamTaskExportQuery request,
        CancellationToken cancellationToken) =>
        Task.FromResult(taskExportRepository.GetExportStreamAsync(request.Filters, cancellationToken));
}
