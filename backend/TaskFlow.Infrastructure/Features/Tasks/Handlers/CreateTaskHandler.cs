using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Common;
using TaskFlow.Application.Tenancy;
using TaskFlow.Application.Tasks;
using TaskFlow.Application.Dashboard;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class CreateTaskHandler(
    TaskFlowDbContext dbContext,
    ICurrentTenant currentTenant,
    IMapper mapper,
    IMemoryCache cache) : IRequestHandler<CreateTaskCommand, TaskDto?>
{
    public async System.Threading.Tasks.Task<TaskDto?> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        if (!currentTenant.IsSet)
        {
            throw new TenantContextMissingException();
        }

        var projectExists = await dbContext.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == request.ProjectId, cancellationToken);

        if (!projectExists) return null;

        var now = DateTime.UtcNow;

        var task = new TaskFlow.Domain.Entities.Task
        {
            Id = Guid.NewGuid(),
            OrganizationId = currentTenant.OrganizationId,
            ProjectId = request.ProjectId,
            Title = request.Title,
            Description = request.Description,
            Status = request.Status,
            Priority = request.Priority,
            DueDateUtc = request.DueDateUtc,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        await dbContext.Tasks.AddAsync(task, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        cache.Remove(DashboardCacheKeys.DashboardStats(currentTenant.OrganizationId));
        return mapper.Map<TaskDto>(task);
    }
}

