using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Auth;
using TaskFlow.Infrastructure.Identity;
using DomainTask = TaskFlow.Domain.Entities.Task;
using DomainTaskPriority = TaskFlow.Domain.Entities.TaskPriority;
using DomainTaskStatus = TaskFlow.Domain.Entities.TaskStatus;

namespace TaskFlow.Infrastructure.Persistence;

public static class IdentityDataSeeder
{
    public static async System.Threading.Tasks.Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var seedOptions = services.GetRequiredService<IOptions<SeedOptions>>().Value;
        var dbContext = services.GetRequiredService<TaskFlowDbContext>();
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
            logger.LogWarning("Admin seed credentials are not configured. Skipping admin user seed.");
        }
        else
        {
            await EnsureAdminAsync(dbContext, userManager, seedOptions, cancellationToken);
        }

        // Ensure any previously created users (before multi-tenancy) get assigned.
        await AssignUsersMissingOrganizationAsync(dbContext, cancellationToken);

        if (seedOptions.DemoDataEnabled)
        {
            await SeedDemoDataAsync(dbContext, userManager, seedOptions, logger, cancellationToken);
        }
    }

    private static async System.Threading.Tasks.Task EnsureAdminAsync(
        TaskFlowDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        SeedOptions seedOptions,
        CancellationToken cancellationToken)
    {
        var defaultOrg = await dbContext.Organizations
            .FirstOrDefaultAsync(o => o.Name == "Default Workspace", cancellationToken);

        if (defaultOrg is null)
        {
            defaultOrg = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Default Workspace",
                JoinCode = "DEFAULT1",
                CreatedAtUtc = DateTime.UtcNow,
            };
            await dbContext.Organizations.AddAsync(defaultOrg, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var adminEmail = seedOptions.AdminEmail!;
        var adminPassword = seedOptions.AdminPassword!;
        var normalizedEmail = adminEmail.ToUpperInvariant();

        var admin = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = adminEmail,
                UserName = adminEmail,
                EmailConfirmed = true,
                CreatedAtUtc = DateTime.UtcNow,
                OrganizationId = defaultOrg.Id,
            };
            var create = await userManager.CreateAsync(admin, adminPassword);
            if (!create.Succeeded)
            {
                return;
            }
        }

        if (admin.OrganizationId == Guid.Empty)
        {
            admin.OrganizationId = defaultOrg.Id;
            await userManager.UpdateAsync(admin);
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

    private static async System.Threading.Tasks.Task AssignUsersMissingOrganizationAsync(TaskFlowDbContext dbContext, CancellationToken cancellationToken)
    {
        var defaultOrg = await dbContext.Organizations.FirstOrDefaultAsync(o => o.Name == "Default Workspace", cancellationToken);
        if (defaultOrg is null)
        {
            return;
        }

        var existingUsersNeedingOrg = await dbContext.Users
            .IgnoreQueryFilters()
            .Where(u => u.OrganizationId == Guid.Empty)
            .ToListAsync(cancellationToken);

        if (existingUsersNeedingOrg.Count > 0)
        {
            foreach (var user in existingUsersNeedingOrg)
            {
                user.OrganizationId = defaultOrg.Id;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static async System.Threading.Tasks.Task SeedDemoDataAsync(
        TaskFlowDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        SeedOptions seedOptions,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        const string seedKey = "demo-data-v1";
        var alreadySeeded = await dbContext.SeedRuns.AnyAsync(s => s.Key == seedKey, cancellationToken);
        if (alreadySeeded)
        {
            logger.LogInformation("Demo seed marker {SeedKey} exists. Skipping bulk seed.", seedKey);
            return;
        }

        var orgCount = Math.Max(1, seedOptions.DemoOrganizationsCount);
        var usersPerOrg = Math.Max(1, seedOptions.DemoUsersPerOrganization);
        var projectsPerOrg = Math.Max(1, seedOptions.DemoProjectsPerOrganization);
        var tasksPerProject = Math.Max(1, seedOptions.DemoTasksPerProject);

        var targetUsers = orgCount * usersPerOrg;
        var existingUsers = await dbContext.Users
            .IgnoreQueryFilters()
            .CountAsync(u => u.Email != null && u.Email.StartsWith("demo.user"), cancellationToken);

        if (existingUsers >= targetUsers)
        {
            logger.LogInformation("Demo data already present ({Users} users). Skipping bulk seed.", existingUsers);
            return;
        }

        var now = DateTime.UtcNow;
        var organizations = new List<Organization>(orgCount);
        for (var i = 1; i <= orgCount; i++)
        {
            var orgName = $"Acme Workspace {i:00}";
            var joinCode = $"ORG{i:000}TF";
            var existing = await dbContext.Organizations.FirstOrDefaultAsync(o => o.Name == orgName, cancellationToken);
            if (existing is null)
            {
                existing = new Organization
                {
                    Id = Guid.NewGuid(),
                    Name = orgName,
                    JoinCode = joinCode,
                    CreatedAtUtc = now.AddDays(-i),
                };
                await dbContext.Organizations.AddAsync(existing, cancellationToken);
            }
            organizations.Add(existing);
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        var random = new Random(42);
        var projectsToAdd = new List<Project>();
        var tasksToAdd = new List<DomainTask>();
        var userIndex = 1;

        foreach (var org in organizations)
        {
            for (var i = 1; i <= usersPerOrg; i++)
            {
                var email = $"demo.user{userIndex:000}@taskflow.local";
                var normalized = email.ToUpperInvariant();
                var existing = await dbContext.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, cancellationToken);

                if (existing is null)
                {
                    var user = new ApplicationUser
                    {
                        Id = Guid.NewGuid(),
                        Email = email,
                        UserName = email,
                        EmailConfirmed = true,
                        CreatedAtUtc = now.AddDays(-random.Next(1, 200)),
                        OrganizationId = org.Id,
                    };

                    var created = await userManager.CreateAsync(user, seedOptions.DemoUserPassword);
                    if (created.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, DomainRoles.User);
                        if (i == 1)
                        {
                            await userManager.AddToRoleAsync(user, DomainRoles.Admin);
                        }
                    }
                }

                userIndex++;
            }

            for (var p = 1; p <= projectsPerOrg; p++)
            {
                var projectName = $"{org.Name} - Project {p:00}";
                var project = await dbContext.Projects
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.OrganizationId == org.Id && x.Name == projectName, cancellationToken);

                if (project is null)
                {
                    var createdAt = now.AddDays(-random.Next(1, 120));
                    project = new Project
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = org.Id,
                        Name = projectName,
                        Description = $"Demo project {p:00} for {org.Name}",
                        CreatedAtUtc = createdAt,
                        UpdatedAtUtc = createdAt,
                    };
                    projectsToAdd.Add(project);
                }
            }
        }

        if (projectsToAdd.Count > 0)
        {
            await dbContext.Projects.AddRangeAsync(projectsToAdd, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var allProjects = await dbContext.Projects
            .IgnoreQueryFilters()
            .Where(p => p.Name.Contains(" - Project "))
            .ToListAsync(cancellationToken);

        foreach (var project in allProjects)
        {
            var existingTaskCount = await dbContext.Tasks
                .IgnoreQueryFilters()
                .CountAsync(t => t.ProjectId == project.Id, cancellationToken);

            for (var t = existingTaskCount + 1; t <= tasksPerProject; t++)
            {
                var createdAt = now.AddDays(-random.Next(1, 90));
                var status = (DomainTaskStatus)random.Next(0, 4);
                var priority = (DomainTaskPriority)random.Next(0, 4);
                tasksToAdd.Add(new DomainTask
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = project.OrganizationId,
                    ProjectId = project.Id,
                    Title = $"Task {t:00} - {project.Name}",
                    Description = "Demo seeded task to showcase realistic dashboard and list behavior.",
                    Status = status,
                    Priority = priority,
                    DueDateUtc = status == DomainTaskStatus.Done ? null : createdAt.AddDays(random.Next(3, 30)),
                    CreatedAtUtc = createdAt,
                    UpdatedAtUtc = createdAt.AddDays(random.Next(0, 10)),
                });
            }
        }

        if (tasksToAdd.Count > 0)
        {
            await dbContext.Tasks.AddRangeAsync(tasksToAdd, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation(
            "Demo seed complete: {Orgs} orgs, {Users} users target, {Projects} projects, {Tasks} tasks added.",
            organizations.Count,
            targetUsers,
            projectsToAdd.Count,
            tasksToAdd.Count);

        await dbContext.SeedRuns.AddAsync(new SeedRun
        {
            Key = seedKey,
            AppliedAtUtc = DateTime.UtcNow,
        }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
