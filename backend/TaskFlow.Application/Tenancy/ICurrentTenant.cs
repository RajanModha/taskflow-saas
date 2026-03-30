namespace TaskFlow.Application.Tenancy;

public interface ICurrentTenant
{
    bool IsSet { get; }
    Guid OrganizationId { get; }
}

