namespace TaskFlow.Domain.Exceptions;

public sealed class BusinessRuleException(string message)
    : AppException(message, 422);
