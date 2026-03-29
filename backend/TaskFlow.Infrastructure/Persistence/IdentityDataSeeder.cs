using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskFlow.Domain.Common;
using TaskFlow.Infrastructure.Auth;
using TaskFlow.Infrastructure.Identity;

namespace TaskFlow.Infrastructure.Persistence;

public static class IdentityDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var seedOptions = services.GetRequiredService<IOptions<SeedOptions>>().Value;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(IdentityDataSeeder));

        foreach (var roleName in DomainRoles.All)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var role = new ApplicationRole { Id = Guid.NewGuid(), Name = roleName };
            var result = await roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                logger.LogError(
                    "Failed to create role {Role}: {Errors}",
                    roleName,
                    string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }

        if (string.IsNullOrWhiteSpace(seedOptions.AdminEmail) ||
            string.IsNullOrWhiteSpace(seedOptions.AdminPassword))
        {
            return;
        }

        var admin = await userManager.FindByEmailAsync(seedOptions.AdminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = seedOptions.AdminEmail,
                UserName = seedOptions.AdminEmail,
                EmailConfirmed = true,
                CreatedAtUtc = DateTime.UtcNow,
            };

            var create = await userManager.CreateAsync(admin, seedOptions.AdminPassword);
            if (!create.Succeeded)
            {
                logger.LogError(
                    "Failed to seed admin user {Email}: {Errors}",
                    seedOptions.AdminEmail,
                    string.Join("; ", create.Errors.Select(e => e.Description)));
                return;
            }
        }

        if (!await userManager.IsInRoleAsync(admin, DomainRoles.Admin))
        {
            await userManager.AddToRoleAsync(admin, DomainRoles.Admin);
        }

        if (!await userManager.IsInRoleAsync(admin, DomainRoles.User))
        {
            await userManager.AddToRoleAsync(admin, DomainRoles.User);
        }
    }
}
