using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.API.ExceptionHandling;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Items["CorrelationId"]?.ToString();
        var traceId = Activity.Current?.Id;

        if (exception is ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            var validationProblem = new ValidationProblemDetails(errors)
            {
                Title = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest,
                Type = "https://httpstatuses.com/400",
                Detail = validationException.Message,
            };
            validationProblem.Extensions["correlationId"] = correlationId;
            validationProblem.Extensions["traceId"] = traceId;

            logger.LogWarning(validationException, "Request validation failed");
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            httpContext.Response.ContentType = "application/problem+json";
            await httpContext.Response.WriteAsJsonAsync(validationProblem, cancellationToken: cancellationToken);
            return true;
        }

        var (status, title, detail, type) = exception switch
        {
            TenantContextMissingException => (
                StatusCodes.Status401Unauthorized,
                "Tenant context missing",
                "Authenticated requests must include tenant context.",
                "https://httpstatuses.com/401"),
            AppException appException => (
                appException.StatusCode,
                "Request failed",
                appException.Message,
                $"https://httpstatuses.com/{appException.StatusCode}"),
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "Unauthorized access.",
                "https://httpstatuses.com/401"),
            OperationCanceledException => (
                499,
                "Request canceled",
                "The client closed the request before completion.",
                "https://httpstatuses.com/499"),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Server error",
                environment.IsDevelopment() ? exception.Message : "An unexpected error occurred.",
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
            Detail = detail,
            Status = status,
            Type = type,
        };
        problem.Extensions["correlationId"] = correlationId;
        problem.Extensions["traceId"] = traceId;

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken: cancellationToken);
        return true;
    }
}
