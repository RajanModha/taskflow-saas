using MediatR;
using TaskFlow.Application.Tasks;

namespace TaskFlow.Application.Workspaces;

public sealed record GetWorkspaceTaskTemplateQuery(Guid UserId, Guid TemplateId) : IRequest<TaskTemplateDto?>;
