using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Auth;
using TaskFlow.Application.Tenancy;
using TaskFlow.Application.Workspaces;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Notifications;
using TaskFlow.Infrastructure.Activity;
using TaskFlow.Infrastructure.Auth;
using TaskFlow.Infrastructure.Identity;
using TaskFlow.Infrastructure.Projects;
using TaskFlow.Infrastructure.Tenancy;
using TaskFlow.Infrastructure.Notifications;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Infrastructure.Services;
using TaskFlow.Infrastructure.Workspaces;

namespace TaskFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtSettings>().Bind(configuration.GetSection(JwtSettings.SectionName));
        services.AddOptions<SeedOptions>().Bind(configuration.GetSection(SeedOptions.SectionName));

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IBoardCacheVersion, BoardCacheVersion>();
        services.AddSingleton<IActivityLogger, ActivityLogger>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentTenant, CurrentTenant>();
        services.AddScoped<ICurrentUser, CurrentUser>();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
        }

        services.AddDbContext<TaskFlowDbContext>(options =>
            options.UseNpgsql(connectionString));

        services
            .AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddEntityFrameworkStores<TaskFlowDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IUserSessionIssuer, UserSessionIssuer>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IWorkspaceService, WorkspaceService>();
        services.AddScoped<IWorkspaceManagementService, WorkspaceManagementService>();
        services.AddScoped<IWorkspaceTagService, WorkspaceTagService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddSingleton<IAppInfo, AppInfoService>();
        services.AddHostedService<RefreshTokenCleanupHostedService>();
        services.AddHostedService<NotificationCleanupHostedService>();
        return services;
    }
}




