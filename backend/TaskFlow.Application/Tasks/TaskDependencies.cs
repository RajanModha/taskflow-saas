using DomainTaskStatus = TaskFlow.Domain.Entities.TaskStatus;
using MediatR;

namespace TaskFlow.Application.Tasks;

public sealed record TaskBlockingSummaryDto(Guid Id, string Title, DomainTaskStatus Status);

public sealed record DependencyDto(Guid BlockedTaskId, TaskBlockingSummaryDto BlockingTask);

public sealed record TaskDependenciesResponse(
    IReadOnlyList<DependencyDto> BlockedBy,
    IReadOnlyList<DependencyDto> Blocking);

public sealed record AddTaskDependencyCommand(Guid TaskId, Guid BlockingTaskId) : IRequest<AddTaskDependencyResult>;

public abstract record AddTaskDependencyResult
{
    public sealed record Ok(DependencyDto Dependency) : AddTaskDependencyResult;
    public sealed record NotFound : AddTaskDependencyResult;
    public sealed record SelfReference : AddTaskDependencyResult;
    public sealed record MaxDependencies : AddTaskDependencyResult;
    public sealed record Duplicate : AddTaskDependencyResult;
    public sealed record Cycle : AddTaskDependencyResult;
}

public sealed record RemoveTaskDependencyCommand(Guid TaskId, Guid BlockingTaskId) : IRequest<bool>;

public sealed record GetTaskDependenciesQuery(Guid TaskId) : IRequest<TaskDependenciesResponse?>;
