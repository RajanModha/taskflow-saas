using MediatR;

namespace TaskFlow.Application.Projects;

public sealed record MilestoneDto(
    Guid Id,
    Guid ProjectId,
    string Name,
    string? Description,
    DateTime? DueDateUtc,
    int TaskCount,
    int CompletedTaskCount,
    decimal Progress,
    DateTime CreatedAt);

public sealed record GetMilestonesQuery(Guid ProjectId) : IRequest<IReadOnlyList<MilestoneDto>?>;

public sealed record CreateMilestoneCommand(
    Guid ProjectId,
    string Name,
    string? Description,
    DateTime? DueDateUtc) : IRequest<MilestoneDto?>;

public sealed record UpdateMilestoneCommand(
    Guid ProjectId,
    Guid MilestoneId,
    string Name,
    string? Description,
    DateTime? DueDateUtc) : IRequest<MilestoneDto?>;

public sealed record DeleteMilestoneCommand(Guid ProjectId, Guid MilestoneId) : IRequest<bool>;
