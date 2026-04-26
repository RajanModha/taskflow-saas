using FluentValidation;
using DomainTaskStatus = TaskFlow.Domain.Entities.TaskStatus;

namespace TaskFlow.Application.Projects.Validators;

public sealed class MoveProjectBoardTaskCommandValidator : AbstractValidator<MoveProjectBoardTaskCommand>
{
    public MoveProjectBoardTaskCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.NewStatus)
            .IsInEnum()
            .Must(s => s is DomainTaskStatus.Todo or DomainTaskStatus.InProgress or DomainTaskStatus.Done or DomainTaskStatus.Cancelled)
            .WithMessage("newStatus must be Todo, InProgress, Done, or Cancelled.");
    }
}
