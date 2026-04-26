namespace TaskFlow.Application.Tasks;

public sealed record TaskAssigneeDto(Guid Id, string UserName, string? DisplayName);
