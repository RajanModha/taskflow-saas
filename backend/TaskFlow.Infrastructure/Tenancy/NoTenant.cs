using TaskFlow.Application.Tenancy;

namespace TaskFlow.Infrastructure.Tenancy;

public sealed class NoTenant : ICurrentTenant
{
    public bool IsSet => false;
    public Guid OrganizationId => Guid.Empty;
}

