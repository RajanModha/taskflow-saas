using MediatR;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Activity;

namespace TaskFlow.Infrastructure.Features.Projects.Handlers;

public sealed class GetProjectActivityHandler(IProjectReadRepository projectRepository)
    : IRequestHandler<GetProjectActivityQuery, PagedResultDto<ActivityLogDto>?>
{
    public async Task<PagedResultDto<ActivityLogDto>?> Handle(
        GetProjectActivityQuery request,
        CancellationToken cancellationToken)
    {
        var paged = await projectRepository.GetProjectActivityAsync(
            request.ProjectId,
            request.Page,
            request.PageSize,
            cancellationToken);
        if (paged is null)
        {
            return null;
        }

        var items = paged.Items.Select(r => new ActivityLogDto(
            r.Id,
            r.Action,
            new ActivityActorDto(r.ActorId, r.ActorName),
            r.OccurredAtUtc,
            ActivityLogMapper.ParseMetadata(r.Metadata)))
            .ToList();
        return PagedResultDto<ActivityLogDto>.Create(items, paged.Page, paged.PageSize, paged.TotalCount);
    }
}
