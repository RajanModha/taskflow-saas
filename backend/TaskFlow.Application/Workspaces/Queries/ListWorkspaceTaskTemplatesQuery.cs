using MediatR;
using TaskFlow.Application.Tasks;

namespace TaskFlow.Application.Workspaces;

public sealed record ListWorkspaceTaskTemplatesQuery(Guid UserId) : IRequest<IReadOnlyList<TaskTemplateDto>?>;
