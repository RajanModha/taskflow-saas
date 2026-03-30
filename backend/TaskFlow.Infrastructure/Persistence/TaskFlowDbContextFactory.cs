using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TaskFlow.Application.Tenancy;
using TaskFlow.Infrastructure.Tenancy;

namespace TaskFlow.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core CLI. Override connection string with env TASKFLOW_CONNECTION_STRING.
/// </summary>
public sealed class TaskFlowDbContextFactory : IDesignTimeDbContextFactory<TaskFlowDbContext>
{
    public TaskFlowDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("TASKFLOW_CONNECTION_STRING")
                               ?? "Host=localhost;Port=5432;Database=task-flow;Username=taskflow;Password=Admin@123";

        var optionsBuilder = new DbContextOptionsBuilder<TaskFlowDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        ICurrentTenant currentTenant = new NoTenant();
        return new TaskFlowDbContext(optionsBuilder.Options, currentTenant);
    }
}
