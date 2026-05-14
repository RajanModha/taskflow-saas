using MediatR;
using TaskFlow.Application.Tasks;

namespace TaskFlow.Application.Workspaces;

public sealed record UpdateWorkspaceTaskTemplateCommand(
    Guid UserId,
    Guid TemplateId,
    UpdateTaskTemplateRequest Request) : IRequest<(int StatusCode, object? Body)>;
