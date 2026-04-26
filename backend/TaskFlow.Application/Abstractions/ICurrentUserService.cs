namespace TaskFlow.Application.Abstractions;

public interface ICurrentUserService
{
    Guid UserId { get; }
    string UserName { get; }
    Guid OrganizationId { get; }
    string Role { get; }
    bool IsAuthenticated { get; }
}
