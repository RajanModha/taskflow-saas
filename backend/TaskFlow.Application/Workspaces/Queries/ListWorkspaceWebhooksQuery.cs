using MediatR;

namespace TaskFlow.Application.Workspaces;

public sealed record ListWorkspaceWebhooksQuery(Guid UserId) : IRequest<IReadOnlyList<WebhookDto>?>;
