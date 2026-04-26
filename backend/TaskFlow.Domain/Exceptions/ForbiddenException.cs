namespace TaskFlow.Domain.Exceptions;

public sealed class ForbiddenException(string message = "Access denied.")
    : AppException(message, 403);
