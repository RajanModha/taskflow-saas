using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Activity;
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
    private static readonly (string FirstName, string LastName)[] SeedPeople =
    [
        ("Jessica", "DeMars"),
        ("Ethan", "Brooks"),
        ("Priya", "Nair"),
        ("Mateo", "Silva"),
        ("Hannah", "Kim"),
        ("Marcus", "Reed"),
        ("Ava", "Patel"),
        ("Noah", "Bennett"),
        ("Chloe", "Fischer"),
        ("Daniel", "Santos"),
        ("Lena", "Morrison"),
        ("Owen", "Campbell"),
    ];

    private static readonly string[] SeedWorkspaceNames =
    [
        "Northstar Logistics",
        "Harbor Health Group",
        "Bluebird Retail",
        "Summit Analytics",
        "Crescent Legal Partners",
        "Ridgeway Manufacturing",
        "Willow Education Services",
        "Atlas Energy Solutions",
        "Orchard Hospitality",
        "Sterling Financial Ops",
        "Nimbus Media Studio",
        "Maple Civic Services",
    ];

    private static readonly (string Name, string Description)[] SeedProjectTemplates =
    [
        ("Q3 Customer Onboarding Refresh", "Improve onboarding journey, reduce setup friction, and tighten handoff between sales and implementation."),
        ("Invoice Automation Rollout", "Automate approval routing, reminders, and posting reconciliation for month-end accounting."),
        ("Support Queue Stabilization", "Reduce ticket aging with clearer triage rules, staffing guardrails, and SLA monitoring."),
        ("Mobile App Reliability Sprint", "Target crash-rate reduction and improve offline data sync reliability for field teams."),
        ("Data Governance Baseline", "Define ownership, retention standards, and audit-ready controls across operational datasets."),
        ("Website Conversion Optimization", "Run structured experiments on landing pages and forms to improve qualified lead conversion."),
    ];

    private static readonly (string Name, string Color)[] SeedTagTemplates =
    [
        ("Backend", "#6366F1"),
        ("Frontend", "#0EA5E9"),
        ("Ops", "#F59E0B"),
        ("Customer", "#22C55E"),
        ("Compliance", "#EF4444"),
        ("Research", "#8B5CF6"),
    ];

    private static readonly (string Name, string Description)[] SeedTaskTemplates =
    [
        ("Draft implementation plan", "Outline scope, dependencies, technical approach, and rollout checkpoints for stakeholder review."),
        ("Run stakeholder discovery interviews", "Collect pain points from customer success, operations, and engineering to validate assumptions."),
        ("Define success metrics", "Set measurable KPIs and reporting cadence to evaluate impact after release."),
        ("Prepare API contract updates", "Document request and response changes, versioning notes, and migration guidance."),
        ("Create QA test matrix", "Cover critical happy paths, edge cases, and regression scenarios with clear expected outcomes."),
        ("Coordinate production rollout window", "Align support coverage, release timing, and rollback communications across teams."),
        ("Review security and access rules", "Validate permissions, tenant boundaries, and audit logging for new workflows."),
        ("Publish release notes draft", "Summarize behavior changes, known limitations, and customer-facing guidance for launch."),
    ];

    private static readonly string[] SeedChecklistTitles =
    [
        "Document acceptance criteria",
        "Confirm owner and due date",
        "Validate staging behavior",
        "Add release note entry",
    ];

    private static readonly string[] SeedCommentTemplates =
    [
        "Reviewed with the operations team. We should keep rollout behind a feature flag for the first week.",
        "Updated implementation notes after QA feedback. No blocking issues so far.",
        "Dependencies are clear now; waiting on API contract approval from platform.",
        "Customer success asked for a short enablement guide before we ship this.",
    ];

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
                EmailVerified = true,
                CreatedAtUtc = DateTime.UtcNow,
                OrganizationId = defaultOrg.Id,
                WorkspaceRole = WorkspaceRole.Owner,
                WorkspaceJoinedAtUtc = DateTime.UtcNow,
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
            admin.WorkspaceJoinedAtUtc = DateTime.UtcNow;
            await userManager.UpdateAsync(admin);
        }

        if (admin.WorkspaceRole != WorkspaceRole.Owner)
        {
            admin.WorkspaceRole = WorkspaceRole.Owner;
            await userManager.UpdateAsync(admin);
        }

        if (admin.WorkspaceJoinedAtUtc == default)
        {
            admin.WorkspaceJoinedAtUtc = admin.CreatedAtUtc;
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

        if (!admin.EmailVerified)
        {
            admin.EmailVerified = true;
            await userManager.UpdateAsync(admin);
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
            var assignedAt = DateTime.UtcNow;
            foreach (var user in existingUsersNeedingOrg)
            {
                user.OrganizationId = defaultOrg.Id;
                user.WorkspaceRole = WorkspaceRole.Member;
                user.WorkspaceJoinedAtUtc = assignedAt;
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
        const string seedKey = "workspace-sample-data-v3";
        var alreadySeeded = await dbContext.SeedRuns.AnyAsync(s => s.Key == seedKey, cancellationToken);
        if (alreadySeeded)
        {
            logger.LogInformation("Seed marker {SeedKey} exists. Skipping bulk seed.", seedKey);
            return;
        }

        var orgCount = Math.Max(1, seedOptions.DemoOrganizationsCount);
        var usersPerOrg = Math.Max(1, seedOptions.DemoUsersPerOrganization);
        var projectsPerOrg = Math.Max(1, seedOptions.DemoProjectsPerOrganization);
        var tasksPerProject = Math.Max(1, seedOptions.DemoTasksPerProject);

        var now = DateTime.UtcNow;
        var organizations = new List<Organization>(orgCount);
        var usersByOrganization = new Dictionary<Guid, List<ApplicationUser>>();

        for (var i = 1; i <= orgCount; i++)
        {
            var baseName = SeedWorkspaceNames[(i - 1) % SeedWorkspaceNames.Length];
            var orgName = orgCount <= SeedWorkspaceNames.Length ? baseName : $"{baseName} {i:00}";
            var joinCode = BuildJoinCode(orgName, i);
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
        var milestonesToAdd = new List<Milestone>();
        var tagsToAdd = new List<Tag>();
        var taskTagsToAdd = new List<TaskTag>();
        var checklistsToAdd = new List<ChecklistItem>();
        var commentsToAdd = new List<Comment>();
        var activitiesToAdd = new List<ActivityLog>();
        var userIndex = 0;
        var projectCursor = 0;

        for (var orgIndex = 0; orgIndex < organizations.Count; orgIndex++)
        {
            var org = organizations[orgIndex];
            var orgUsers = new List<ApplicationUser>();
            for (var i = 1; i <= usersPerOrg; i++)
            {
                var person = SeedPeople[userIndex % SeedPeople.Length];
                var emailLocalPart = $"{person.FirstName}.{person.LastName}.{orgIndex + 1:00}{i:00}".ToLowerInvariant();
                var email = $"{emailLocalPart}@taskflow.local";
                var normalized = email.ToUpperInvariant();
                var existing = await dbContext.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, cancellationToken);

                if (existing is null)
                {
                    var createdAt = now.AddDays(-random.Next(1, 200));
                    var user = new ApplicationUser
                    {
                        Id = Guid.NewGuid(),
                        Email = email,
                        UserName = emailLocalPart,
                        DisplayName = $"{person.FirstName} {person.LastName}",
                        EmailConfirmed = true,
                        EmailVerified = true,
                        CreatedAtUtc = createdAt,
                        OrganizationId = org.Id,
                        WorkspaceRole = i == 1 ? WorkspaceRole.Owner : WorkspaceRole.Member,
                        WorkspaceJoinedAtUtc = createdAt,
                    };

                    var created = await userManager.CreateAsync(user, seedOptions.DemoUserPassword);
                    if (created.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, DomainRoles.User);
                        if (i == 1)
                        {
                            await userManager.AddToRoleAsync(user, DomainRoles.Admin);
                        }
                        orgUsers.Add(user);
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(existing.DisplayName))
                    {
                        existing.DisplayName = $"{person.FirstName} {person.LastName}";
                        await userManager.UpdateAsync(existing);
                    }
                    orgUsers.Add(existing);
                }

                userIndex++;
            }
            usersByOrganization[org.Id] = orgUsers;

            for (var p = 1; p <= projectsPerOrg; p++)
            {
                var template = SeedProjectTemplates[projectCursor % SeedProjectTemplates.Length];
                projectCursor++;
                var projectName = projectsPerOrg <= SeedProjectTemplates.Length
                    ? template.Name
                    : $"{template.Name} #{p:00}";

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
                        Description = template.Description,
                        CreatedAtUtc = createdAt,
                        UpdatedAtUtc = createdAt,
                    };
                    projectsToAdd.Add(project);
                }
            }

            foreach (var (name, color) in SeedTagTemplates)
            {
                var normalizedTag = name.ToUpperInvariant();
                var existingTag = await dbContext.Tags
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(
                        t => t.OrganizationId == org.Id && t.NormalizedName == normalizedTag,
                        cancellationToken);
                if (existingTag is not null)
                {
                    continue;
                }

                tagsToAdd.Add(new Tag
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = org.Id,
                    Name = name,
                    NormalizedName = normalizedTag,
                    Color = color,
                    CreatedAtUtc = now.AddDays(-random.Next(1, 120)),
                });
            }
        }

        if (projectsToAdd.Count > 0)
        {
            await dbContext.Projects.AddRangeAsync(projectsToAdd, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (tagsToAdd.Count > 0)
        {
            await dbContext.Tags.AddRangeAsync(tagsToAdd, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var organizationIds = organizations.Select(o => o.Id).ToList();

        var allProjects = await dbContext.Projects
            .IgnoreQueryFilters()
            .Where(p => organizationIds.Contains(p.OrganizationId))
            .ToListAsync(cancellationToken);

        foreach (var project in allProjects)
        {
            var milestoneA = await dbContext.Milestones
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.ProjectId == project.Id && m.Name == "Phase 1", cancellationToken);
            if (milestoneA is null)
            {
                milestoneA = new Milestone
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = project.OrganizationId,
                    ProjectId = project.Id,
                    Name = "Phase 1",
                    Description = "Initial implementation and internal validation milestone.",
                    DueDateUtc = now.AddDays(random.Next(10, 35)),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                };
                milestonesToAdd.Add(milestoneA);
            }

            var milestoneB = await dbContext.Milestones
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.ProjectId == project.Id && m.Name == "Phase 2", cancellationToken);
            if (milestoneB is null)
            {
                milestoneB = new Milestone
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = project.OrganizationId,
                    ProjectId = project.Id,
                    Name = "Phase 2",
                    Description = "External rollout and stabilization milestone.",
                    DueDateUtc = now.AddDays(random.Next(30, 70)),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                };
                milestonesToAdd.Add(milestoneB);
            }

            var existingTaskCount = await dbContext.Tasks
                .IgnoreQueryFilters()
                .CountAsync(t => t.ProjectId == project.Id, cancellationToken);

            for (var t = existingTaskCount + 1; t <= tasksPerProject; t++)
            {
                var template = SeedTaskTemplates[(t - 1) % SeedTaskTemplates.Length];
                var createdAt = now.AddDays(-random.Next(1, 90));
                var statusRoll = random.Next(100);
                var status = statusRoll switch
                {
                    < 15 => DomainTaskStatus.Backlog,
                    < 40 => DomainTaskStatus.Todo,
                    < 70 => DomainTaskStatus.InProgress,
                    < 92 => DomainTaskStatus.Done,
                    _ => DomainTaskStatus.Cancelled,
                };

                var priorityRoll = random.Next(100);
                var priority = priorityRoll switch
                {
                    < 40 => DomainTaskPriority.Low,
                    < 75 => DomainTaskPriority.Medium,
                    < 92 => DomainTaskPriority.High,
                    _ => DomainTaskPriority.Urgent,
                };
                var orgUsers = usersByOrganization.GetValueOrDefault(project.OrganizationId) ?? new List<ApplicationUser>();
                var assignee = orgUsers.Count > 0 && status != DomainTaskStatus.Backlog
                    ? orgUsers[random.Next(orgUsers.Count)]
                    : null;

                Guid? milestoneId = status is DomainTaskStatus.Done or DomainTaskStatus.InProgress
                    ? (random.Next(0, 2) == 0 ? milestoneA.Id : milestoneB.Id)
                    : null;

                DateTime? dueDate = null;
                if (status is not DomainTaskStatus.Done and not DomainTaskStatus.Cancelled)
                {
                    var dueRoll = random.Next(100);
                    dueDate = dueRoll switch
                    {
                        < 20 => now.AddDays(-random.Next(1, 10)),   // overdue
                        < 45 => now.AddDays(random.Next(0, 3)),     // due soon
                        < 90 => now.AddDays(random.Next(4, 30)),    // upcoming
                        _ => null,                                   // no due date
                    };
                }

                var task = new DomainTask
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = project.OrganizationId,
                    ProjectId = project.Id,
                    Title = $"{template.Name} - {project.Name}",
                    Description = template.Description,
                    Status = status,
                    Priority = priority,
                    DueDateUtc = dueDate,
                    CreatedAtUtc = createdAt,
                    UpdatedAtUtc = createdAt.AddDays(random.Next(0, 10)),
                    AssigneeId = assignee?.Id,
                    MilestoneId = milestoneId,
                };
                tasksToAdd.Add(task);

                if (assignee is not null)
                {
                    activitiesToAdd.Add(new ActivityLog
                    {
                        Id = Guid.NewGuid(),
                        EntityType = "Task",
                        EntityId = task.Id,
                        Action = ActivityActions.TaskCreated,
                        ActorId = assignee.Id,
                        ActorName = assignee.DisplayName ?? assignee.UserName ?? "Team Member",
                        OccurredAtUtc = createdAt,
                        OrganizationId = task.OrganizationId,
                    });
                }
            }
        }

        if (milestonesToAdd.Count > 0)
        {
            await dbContext.Milestones.AddRangeAsync(milestonesToAdd, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (tasksToAdd.Count > 0)
        {
            await dbContext.Tasks.AddRangeAsync(tasksToAdd, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var tagsByOrg = await dbContext.Tags
            .IgnoreQueryFilters()
            .Where(t => organizationIds.Contains(t.OrganizationId))
            .GroupBy(t => t.OrganizationId)
            .ToDictionaryAsync(g => g.Key, g => g.ToList(), cancellationToken);

        foreach (var task in tasksToAdd)
        {
            if (!tagsByOrg.TryGetValue(task.OrganizationId, out var availableTags) || availableTags.Count == 0)
            {
                continue;
            }

            var tagCount = random.Next(0, 4);
            var chosenTags = availableTags
                .OrderBy(_ => random.Next())
                .Take(tagCount)
                .ToList();

            foreach (var tag in chosenTags)
            {
                taskTagsToAdd.Add(new TaskTag
                {
                    TaskId = task.Id,
                    TagId = tag.Id,
                });
            }
        }

        if (taskTagsToAdd.Count > 0)
        {
            await dbContext.TaskTags.AddRangeAsync(taskTagsToAdd, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        foreach (var task in tasksToAdd)
        {
            var orgUsers = usersByOrganization.GetValueOrDefault(task.OrganizationId) ?? new List<ApplicationUser>();
            if (orgUsers.Count == 0)
            {
                continue;
            }

            var actor = task.AssigneeId.HasValue
                ? orgUsers.FirstOrDefault(u => u.Id == task.AssigneeId.Value) ?? orgUsers[0]
                : orgUsers[0];
            var actorName = actor.DisplayName ?? actor.UserName ?? "Team Member";

            var checklistCount = random.Next(0, 4);
            for (var i = 0; i < checklistCount; i++)
            {
                var completed = task.Status == DomainTaskStatus.Done || (task.Status == DomainTaskStatus.InProgress && random.Next(100) < 45);
                checklistsToAdd.Add(new ChecklistItem
                {
                    Id = Guid.NewGuid(),
                    TaskId = task.Id,
                    Title = SeedChecklistTitles[(i + random.Next(SeedChecklistTitles.Length)) % SeedChecklistTitles.Length],
                    Order = i + 1,
                    IsCompleted = completed,
                    CreatedAtUtc = task.CreatedAtUtc.AddHours(i),
                    CompletedAtUtc = completed ? task.UpdatedAtUtc : null,
                });

                if (completed)
                {
                    activitiesToAdd.Add(new ActivityLog
                    {
                        Id = Guid.NewGuid(),
                        EntityType = "Task",
                        EntityId = task.Id,
                        Action = ActivityActions.TaskChecklistItemCompleted,
                        ActorId = actor.Id,
                        ActorName = actorName,
                        OccurredAtUtc = task.UpdatedAtUtc,
                        OrganizationId = task.OrganizationId,
                    });
                }
            }

            var commentCount = random.Next(0, 3);
            for (var i = 0; i < commentCount; i++)
            {
                var commenter = orgUsers[random.Next(orgUsers.Count)];
                var commenterName = commenter.DisplayName ?? commenter.UserName ?? "Team Member";
                var created = task.CreatedAtUtc.AddDays(random.Next(0, 12));
                var content = SeedCommentTemplates[(i + random.Next(SeedCommentTemplates.Length)) % SeedCommentTemplates.Length];
                commentsToAdd.Add(new Comment
                {
                    Id = Guid.NewGuid(),
                    TaskId = task.Id,
                    AuthorId = commenter.Id,
                    Content = content,
                    CreatedAtUtc = created,
                    UpdatedAtUtc = created,
                    IsEdited = false,
                    IsDeleted = false,
                });

                activitiesToAdd.Add(new ActivityLog
                {
                    Id = Guid.NewGuid(),
                    EntityType = "Task",
                    EntityId = task.Id,
                    Action = ActivityActions.TaskCommented,
                    ActorId = commenter.Id,
                    ActorName = commenterName,
                    OccurredAtUtc = created,
                    OrganizationId = task.OrganizationId,
                });
            }

            if (task.Status == DomainTaskStatus.Done)
            {
                activitiesToAdd.Add(new ActivityLog
                {
                    Id = Guid.NewGuid(),
                    EntityType = "Task",
                    EntityId = task.Id,
                    Action = ActivityActions.TaskStatusChanged,
                    ActorId = actor.Id,
                    ActorName = actorName,
                    OccurredAtUtc = task.UpdatedAtUtc,
                    Metadata = "{\"from\":\"InProgress\",\"to\":\"Done\"}",
                    OrganizationId = task.OrganizationId,
                });
            }
        }

        if (checklistsToAdd.Count > 0)
        {
            await dbContext.ChecklistItems.AddRangeAsync(checklistsToAdd, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (commentsToAdd.Count > 0)
        {
            await dbContext.Comments.AddRangeAsync(commentsToAdd, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (activitiesToAdd.Count > 0)
        {
            await dbContext.ActivityLogs.AddRangeAsync(activitiesToAdd, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation(
            "Seed complete: {Orgs} orgs, {Users} users target, {Projects} projects, {Tasks} tasks, {Tags} tags, {Milestones} milestones, {ChecklistItems} checklist items, {Comments} comments, {Activities} activity rows added.",
            organizations.Count,
            orgCount * usersPerOrg,
            projectsToAdd.Count,
            tasksToAdd.Count,
            tagsToAdd.Count,
            milestonesToAdd.Count,
            checklistsToAdd.Count,
            commentsToAdd.Count,
            activitiesToAdd.Count);

        await dbContext.SeedRuns.AddAsync(new SeedRun
        {
            Key = seedKey,
            AppliedAtUtc = DateTime.UtcNow,
        }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string BuildJoinCode(string orgName, int index)
    {
        var letters = new string(orgName.Where(char.IsLetter).Take(6).ToArray()).ToUpperInvariant();
        if (letters.Length < 4)
        {
            letters = $"{letters}TASKFLOW";
        }

        return $"{letters[..4]}{index:000}";
    }
}
