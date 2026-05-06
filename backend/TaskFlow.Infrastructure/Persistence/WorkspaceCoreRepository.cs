using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Infrastructure.Persistence;

public sealed class WorkspaceCoreRepository(TaskFlowDbContext dbContext) : IWorkspaceCoreRepository
{
    public async Task<bool> JoinCodeExistsAsync(string joinCode, CancellationToken cancellationToken) =>
        await dbContext.Organizations.AsNoTracking().AnyAsync(o => o.JoinCode == joinCode, cancellationToken);

    public async Task<Guid> CreateOrganizationAsync(
        string name,
        string joinCode,
        DateTime createdAtUtc,
        CancellationToken cancellationToken)
    {
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = name,
            JoinCode = joinCode,
            CreatedAtUtc = createdAtUtc,
        };

        await dbContext.Organizations.AddAsync(organization, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return organization.Id;
    }

    public async Task<WorkspaceOrganizationReadModel?> GetOrganizationByJoinCodeAsync(
        string joinCode,
        CancellationToken cancellationToken) =>
        await dbContext.Organizations.AsNoTracking()
            .Where(o => o.JoinCode == joinCode)
            .Select(o => new WorkspaceOrganizationReadModel(o.Id, o.Name, o.JoinCode))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<WorkspaceAdminReadModel>> GetOrganizationAdminsAsync(
        Guid organizationId,
        CancellationToken cancellationToken) =>
        await dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => u.OrganizationId == organizationId &&
                        (u.WorkspaceRole == WorkspaceRole.Owner || u.WorkspaceRole == WorkspaceRole.Admin))
            .Select(u => new WorkspaceAdminReadModel(u.Id, u.OrganizationId))
            .ToListAsync(cancellationToken);
}
