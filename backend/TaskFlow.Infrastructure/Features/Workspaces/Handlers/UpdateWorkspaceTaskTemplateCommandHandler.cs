using MediatR;
using TaskFlow.Application.Tasks;
using TaskFlow.Application.Workspaces;

namespace TaskFlow.Infrastructure.Features.Workspaces.Handlers;

public sealed class UpdateWorkspaceTaskTemplateCommandHandler(IWorkspaceTaskTemplateService templates)
    : IRequestHandler<UpdateWorkspaceTaskTemplateCommand, (int StatusCode, object? Body)>
{
    public Task<(int StatusCode, object? Body)> Handle(
        UpdateWorkspaceTaskTemplateCommand request,
        CancellationToken cancellationToken) =>
        templates.UpdateTemplateAsync(request.UserId, request.TemplateId, request.Request, cancellationToken);
}
