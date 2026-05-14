using MediatR;
using TaskFlow.Application.Tasks;
using TaskFlow.Application.Workspaces;

namespace TaskFlow.Infrastructure.Features.Workspaces.Handlers;

public sealed class ListWorkspaceTaskTemplatesQueryHandler(IWorkspaceTaskTemplateService templates)
    : IRequestHandler<ListWorkspaceTaskTemplatesQuery, IReadOnlyList<TaskTemplateDto>?>
{
    public Task<IReadOnlyList<TaskTemplateDto>?> Handle(
        ListWorkspaceTaskTemplatesQuery request,
        CancellationToken cancellationToken) =>
        templates.ListTemplatesAsync(request.UserId, cancellationToken);
}
