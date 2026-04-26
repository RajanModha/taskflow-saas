using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Projects;
using TaskFlow.Infrastructure.Features.Tasks;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Projects.Handlers;

public sealed class GetProjectExportHandler(
    TaskFlowDbContext dbContext,
    IMapper mapper,
    ICurrentUserService currentUser)
    : IRequestHandler<GetProjectExportQuery, GetProjectExportQueryResponse>
{
    public async Task<GetProjectExportQueryResponse> Handle(
        GetProjectExportQuery request,
        CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken);

        if (project is null)
        {
            return new GetProjectExportQueryResponse(true, false, null);
        }

        var query = dbContext.Tasks
            .AsNoTracking()
            .Where(t => t.ProjectId == request.ProjectId && !t.IsDeleted);

        var count = await query.LongCountAsync(cancellationToken);
        if (count > 10_000)
        {
            return new GetProjectExportQueryResponse(false, true, null);
        }

        var tasks = await query
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var taskDtos = await TaskProjection.ToDtosAsync(dbContext, tasks, cancellationToken);
        var projectDto = mapper.Map<ProjectDto>(project);
        var exportedBy = string.IsNullOrWhiteSpace(currentUser.UserName)
            ? currentUser.UserId.ToString()
            : currentUser.UserName;

        var payload = new ProjectExportPayload(
            projectDto,
            taskDtos,
            DateTime.UtcNow,
            exportedBy,
            (int)count);

        return new GetProjectExportQueryResponse(false, false, payload);
    }
}
