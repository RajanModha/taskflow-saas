using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Validation;

namespace TaskFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
        return services;
    }
}
