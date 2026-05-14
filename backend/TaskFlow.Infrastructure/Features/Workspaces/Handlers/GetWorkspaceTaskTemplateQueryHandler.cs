using MediatR;
using TaskFlow.Application.Tasks;
using TaskFlow.Application.Workspaces;

namespace TaskFlow.Infrastructure.Features.Workspaces.Handlers;

public sealed class GetWorkspaceTaskTemplateQueryHandler(IWorkspaceTaskTemplateService templates)
    : IRequestHandler<GetWorkspaceTaskTemplateQuery, TaskTemplateDto?>
{
    public Task<TaskTemplateDto?> Handle(
        GetWorkspaceTaskTemplateQuery request,
        CancellationToken cancellationToken) =>
        templates.GetTemplateAsync(request.UserId, request.TemplateId, cancellationToken);
}
