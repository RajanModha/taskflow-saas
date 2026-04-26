using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Common;

namespace TaskFlow.API.ExceptionHandling;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            var validationProblem = new ProblemDetails
            {
                Title = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest,
                Type = "https://httpstatuses.com/400",
                Detail = environment.IsDevelopment() ? validationException.Message : null,
                Extensions = { ["errors"] = errors },
            };

            logger.LogWarning(validationException, "Request validation failed");
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            httpContext.Response.ContentType = "application/problem+json";
            await httpContext.Response.WriteAsJsonAsync(validationProblem, cancellationToken: cancellationToken);
            return true;
        }

        var (status, title, type) = exception switch
        {
            TenantContextMissingException => (
                StatusCodes.Status401Unauthorized,
                "Tenant context missing",
                "https://httpstatuses.com/401"),
            UnauthorizedAccessException => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                "https://httpstatuses.com/403"),
            InvalidOperationException => (
                StatusCodes.Status500InternalServerError,
                "Server error",
                "https://httpstatuses.com/500"),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Server error",
                "https://httpstatuses.com/500"),
        };

        if (status >= 500)
        {
            logger.LogError(exception, "Unhandled exception");
        }
        else
        {
            logger.LogWarning(exception, "Handled application exception");
        }

        var problem = new ProblemDetails
        {
            Title = title,
            Detail = environment.IsDevelopment()
                ? exception.Message
                : "An unexpected error occurred.",
            Status = status,
            Type = type,
        };

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken: cancellationToken);
        return true;
    }
}
