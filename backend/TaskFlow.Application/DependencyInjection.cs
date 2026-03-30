using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Mapping;
using TaskFlow.Application.Validation;

namespace TaskFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
        services.AddAutoMapper(typeof(TaskFlowMappingProfile).Assembly);
        return services;
    }
}
