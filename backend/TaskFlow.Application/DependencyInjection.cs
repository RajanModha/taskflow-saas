using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Mapping;
using TaskFlow.Application.Validation;

namespace TaskFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

        // Manual AutoMapper wiring to avoid relying on the (archived) DI extension package.
        services.AddSingleton(sp =>
        {
            var configExpression = new MapperConfigurationExpression();
            configExpression.AddProfile<TaskFlowMappingProfile>();

            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var config = new MapperConfiguration(configExpression, loggerFactory);
            config.AssertConfigurationIsValid();
            return config;
        });
        services.AddSingleton<IMapper>(sp => sp.GetRequiredService<MapperConfiguration>().CreateMapper());

        return services;
    }
}
