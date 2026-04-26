namespace TaskFlow.Domain.Exceptions;

public sealed class NotFoundException(string resource, object id)
    : AppException($"{resource} with id '{id}' was not found.", 404);
