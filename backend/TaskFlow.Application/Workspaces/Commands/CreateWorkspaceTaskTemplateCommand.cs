using MediatR;
using TaskFlow.Application.Tasks;

namespace TaskFlow.Application.Workspaces;

public sealed record CreateWorkspaceTaskTemplateCommand(Guid UserId, CreateTaskTemplateRequest Request)
    : IRequest<(int StatusCode, object? Body)>;
