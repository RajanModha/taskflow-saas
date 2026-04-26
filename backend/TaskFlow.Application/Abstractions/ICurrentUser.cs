namespace TaskFlow.Application.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }
}
