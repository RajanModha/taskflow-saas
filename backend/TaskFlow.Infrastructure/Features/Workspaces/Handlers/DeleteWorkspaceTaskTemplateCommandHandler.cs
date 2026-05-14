using MediatR;
using TaskFlow.Application.Workspaces;

namespace TaskFlow.Infrastructure.Features.Workspaces.Handlers;

public sealed class DeleteWorkspaceTaskTemplateCommandHandler(IWorkspaceTaskTemplateService templates)
    : IRequestHandler<DeleteWorkspaceTaskTemplateCommand, int>
{
    public Task<int> Handle(DeleteWorkspaceTaskTemplateCommand request, CancellationToken cancellationToken) =>
        templates.DeleteTemplateAsync(request.UserId, request.TemplateId, cancellationToken);
}
