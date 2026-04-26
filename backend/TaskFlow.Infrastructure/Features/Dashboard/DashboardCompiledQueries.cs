using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Persistence;
using DomainTaskStatus = TaskFlow.Domain.Entities.TaskStatus;

namespace TaskFlow.Infrastructure.Features.Dashboard;

internal readonly record struct StatusPriorityCountRow(DomainTaskStatus Status, TaskPriority Priority, int Count);

internal static class DashboardCompiledQueries
{
    /// <summary>
    /// Single grouped scan over tasks for the current tenant (global query filter) to derive status and priority counts.
    /// </summary>
    internal static readonly Func<TaskFlowDbContext, IAsyncEnumerable<StatusPriorityCountRow>> StatusPriorityGroups =
        EF.CompileAsyncQuery((TaskFlowDbContext ctx) =>
            ctx.Tasks
                .AsNoTracking()
                .GroupBy(t => new { t.Status, t.Priority })
                .Select(g => new StatusPriorityCountRow(
                    g.Key.Status,
                    g.Key.Priority,
                    g.Count())));
}
