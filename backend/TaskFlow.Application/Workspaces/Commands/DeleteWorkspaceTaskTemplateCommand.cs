using MediatR;

namespace TaskFlow.Application.Workspaces;

public sealed record DeleteWorkspaceTaskTemplateCommand(Guid UserId, Guid TemplateId) : IRequest<int>;
