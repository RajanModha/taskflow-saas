using MediatR;
using TaskFlow.Application.Tasks;
using TaskFlow.Application.Workspaces;

namespace TaskFlow.Infrastructure.Features.Workspaces.Handlers;

public sealed class CreateWorkspaceTaskTemplateCommandHandler(IWorkspaceTaskTemplateService templates)
    : IRequestHandler<CreateWorkspaceTaskTemplateCommand, (int StatusCode, object? Body)>
{
    public Task<(int StatusCode, object? Body)> Handle(
        CreateWorkspaceTaskTemplateCommand request,
        CancellationToken cancellationToken) =>
        templates.CreateTemplateAsync(request.UserId, request.Request, cancellationToken);
}
