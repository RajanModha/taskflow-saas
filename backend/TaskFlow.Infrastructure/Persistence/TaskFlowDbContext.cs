using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using TaskFlow.Application.Tenancy;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Identity;

namespace TaskFlow.Infrastructure.Persistence;

public sealed class TaskFlowDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    private readonly ICurrentTenant _currentTenant;

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TaskFlow.Domain.Entities.Task> Tasks => Set<TaskFlow.Domain.Entities.Task>();

    public DbSet<Comment> Comments => Set<Comment>();

    public DbSet<Tag> Tags => Set<Tag>();

    public DbSet<TaskTag> TaskTags => Set<TaskTag>();

    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();

    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<SeedRun> SeedRuns => Set<SeedRun>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<PendingInvite> PendingInvites => Set<PendingInvite>();

    public TaskFlowDbContext(
        DbContextOptions<TaskFlowDbContext> options,
        ICurrentTenant currentTenant)
        : base(options)
    {
        _currentTenant = currentTenant;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Organization>(entity =>
        {
            entity.Property(o => o.Name).HasMaxLength(128).IsRequired();
            entity.Property(o => o.JoinCode).HasMaxLength(32).IsRequired();
            entity.Property(o => o.CreatedAtUtc).IsRequired();
            entity.HasIndex(o => o.JoinCode).IsUnique();
        });

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.UserName).HasMaxLength(64);
            entity.Property(u => u.NormalizedUserName).HasMaxLength(64);
            entity.Property(u => u.Email).HasMaxLength(256);
            entity.Property(u => u.NormalizedEmail).HasMaxLength(256);
            entity.Property(u => u.CreatedAtUtc).IsRequired();

            entity.Property(u => u.DisplayName).HasMaxLength(50);
            entity.Property(u => u.AvatarUrl).HasMaxLength(2048);

            entity.Property(u => u.WorkspaceRole).IsRequired();
            entity.Property(u => u.WorkspaceJoinedAtUtc).IsRequired();

            entity.Property(u => u.OrganizationId).IsRequired();
            entity.Property(u => u.EmailVerificationToken).HasMaxLength(64);
            entity.Property(u => u.PasswordResetToken).HasMaxLength(64);
            entity.HasOne<Organization>()
                .WithMany()
                .HasForeignKey(u => u.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Tenant isolation: all org-scoped entities should be filtered by org_id.
            entity.HasQueryFilter(u => _currentTenant.IsSet && u.OrganizationId == _currentTenant.OrganizationId);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.Property(t => t.TokenHash).HasMaxLength(64).IsRequired();
            entity.Property(t => t.ReplacedByTokenHash).HasMaxLength(64);
            entity.Property(t => t.DeviceInfo).HasMaxLength(1024);
            entity.Property(t => t.IpAddress).HasMaxLength(64);
            entity.Property(t => t.CreatedAtUtc).IsRequired();
            entity.Property(t => t.ExpiresAtUtc).IsRequired();
            entity.HasIndex(t => t.TokenHash);
            entity.HasIndex(t => t.UserId);
            entity
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Project>(entity =>
        {
            entity.Property(p => p.Name).HasMaxLength(160).IsRequired();
            entity.Property(p => p.Description).HasMaxLength(2000);
            entity.Property(p => p.CreatedAtUtc).IsRequired();
            entity.Property(p => p.UpdatedAtUtc).IsRequired();
            entity.Property(p => p.IsDeleted).IsRequired().HasDefaultValue(false);

            entity.Property(p => p.OrganizationId).IsRequired();

            // Fail-closed tenant filter: no tenant context means no data returned.
            entity.HasQueryFilter(p => _currentTenant.IsSet && p.OrganizationId == _currentTenant.OrganizationId);
            entity.HasIndex(p => p.OrganizationId);
            entity.HasIndex(p => p.IsDeleted);
            entity.HasIndex(p => new { p.Id, p.OrganizationId }).IsUnique();
        });

        builder.Entity<TaskFlow.Domain.Entities.Task>(entity =>
        {
            entity.Property(t => t.Title).HasMaxLength(200).IsRequired();
            entity.Property(t => t.Description).HasMaxLength(4000);
            entity.Property(t => t.Status).IsRequired();
            entity.Property(t => t.Priority).IsRequired();
            entity.Property(t => t.DueDateUtc);
            entity.Property(t => t.CreatedAtUtc).IsRequired();
            entity.Property(t => t.UpdatedAtUtc).IsRequired();

            entity.Property(t => t.OrganizationId).IsRequired();
            entity.Property(t => t.ReminderSent).IsRequired().HasDefaultValue(false);
            entity.Property(t => t.IsDeleted).IsRequired().HasDefaultValue(false);
            entity.Property(t => t.RowVersion)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasQueryFilter(t => _currentTenant.IsSet && t.OrganizationId == _currentTenant.OrganizationId);
            entity.HasIndex(t => t.OrganizationId);
            entity.HasIndex(t => t.IsDeleted);
            entity.HasIndex(t => new { t.OrganizationId, t.ProjectId });
            entity.HasIndex(t => new { t.OrganizationId, t.CreatedAtUtc });
            entity.HasIndex(t => new { t.OrganizationId, t.DueDateUtc });

            entity
                .HasOne(d => d.Project)
                .WithMany()
                // Enforce tenant-safe relationship: task can only point to project in same org.
                .HasForeignKey(t => new { t.ProjectId, t.OrganizationId })
                .HasPrincipalKey(nameof(Project.Id), nameof(Project.OrganizationId))
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(t => t.AssigneeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Comment>(entity =>
        {
            entity.Property(c => c.Content).HasMaxLength(4000).IsRequired();
            entity.Property(c => c.CreatedAtUtc).IsRequired();
            entity.Property(c => c.UpdatedAtUtc).IsRequired();
            entity.Property(c => c.IsEdited).IsRequired();
            entity.Property(c => c.IsDeleted).IsRequired().HasDefaultValue(false);

            entity
                .HasOne(c => c.ParentTask)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(
                c => _currentTenant.IsSet && c.ParentTask.OrganizationId == _currentTenant.OrganizationId);

            entity.HasIndex(c => new { c.TaskId, c.CreatedAtUtc })
                .IsDescending(false, true);
        });

        builder.Entity<Tag>(entity =>
        {
            entity.Property(t => t.Name).HasMaxLength(30).IsRequired();
            entity.Property(t => t.NormalizedName).HasMaxLength(30).IsRequired();
            entity.Property(t => t.Color).HasMaxLength(7).IsRequired();
            entity.Property(t => t.CreatedAtUtc).IsRequired();
            entity.Property(t => t.OrganizationId).IsRequired();

            entity
                .HasOne<Organization>()
                .WithMany()
                .HasForeignKey(t => t.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(t => _currentTenant.IsSet && t.OrganizationId == _currentTenant.OrganizationId);
            entity.HasIndex(t => new { t.OrganizationId, t.NormalizedName }).IsUnique();
        });

        builder.Entity<TaskTag>(entity =>
        {
            entity.HasKey(x => new { x.TaskId, x.TagId });

            entity
                .HasOne(x => x.ParentTask)
                .WithMany(t => t.TaskTags)
                .HasForeignKey(x => x.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.Tag)
                .WithMany(t => t.TaskTags)
                .HasForeignKey(x => x.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(
                x => _currentTenant.IsSet && x.ParentTask.OrganizationId == _currentTenant.OrganizationId);
        });

        builder.Entity<ChecklistItem>(entity =>
        {
            entity.Property(c => c.Title).HasMaxLength(200).IsRequired();
            entity.Property(c => c.IsCompleted).IsRequired();
            entity.Property(c => c.Order).HasColumnName("item_order");
            entity.Property(c => c.CreatedAtUtc).IsRequired();

            entity
                .HasOne(c => c.ParentTask)
                .WithMany(t => t.ChecklistItems)
                .HasForeignKey(c => c.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(
                c => _currentTenant.IsSet && c.ParentTask.OrganizationId == _currentTenant.OrganizationId);

            entity.HasIndex(c => new { c.TaskId, c.Order });
        });

        builder.Entity<ActivityLog>(entity =>
        {
            entity.Property(a => a.EntityType).HasMaxLength(32).IsRequired();
            entity.Property(a => a.Action).HasMaxLength(80).IsRequired();
            entity.Property(a => a.ActorName).HasMaxLength(256).IsRequired();
            entity.Property(a => a.OccurredAtUtc).IsRequired();
            entity.Property(a => a.Metadata).HasMaxLength(4000);
            entity.Property(a => a.OrganizationId).IsRequired();

            entity
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(a => a.ActorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(
                a => _currentTenant.IsSet && a.OrganizationId == _currentTenant.OrganizationId);

            entity.HasIndex(e => new { e.EntityType, e.EntityId, e.OccurredAtUtc })
                .IsDescending(false, false, true);

            entity.HasIndex(a => a.OrganizationId);
        });

        builder.Entity<Notification>(entity =>
        {
            entity.Property(n => n.Type).HasMaxLength(80).IsRequired();
            entity.Property(n => n.Title).HasMaxLength(160).IsRequired();
            entity.Property(n => n.Body).HasMaxLength(1000).IsRequired();
            entity.Property(n => n.EntityType).HasMaxLength(32);
            entity.Property(n => n.IsRead).HasDefaultValue(false).IsRequired();
            entity.Property(n => n.CreatedAt).IsRequired();

            entity
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt })
                .IsDescending(false, false, true);
        });

        builder.Entity<PendingInvite>(entity =>
        {
            entity.Property(i => i.Email).HasMaxLength(256).IsRequired();
            entity.Property(i => i.NormalizedEmail).HasMaxLength(256).IsRequired();
            entity.Property(i => i.Role).IsRequired();
            entity.Property(i => i.TokenHash).HasMaxLength(64).IsRequired();
            entity.Property(i => i.ExpiresAtUtc).IsRequired();
            entity.Property(i => i.SentAtUtc).IsRequired();

            entity.HasIndex(i => i.TokenHash).IsUnique();
            entity.HasIndex(i => new { i.OrganizationId, i.Email });

            entity.HasQueryFilter(i => _currentTenant.IsSet && i.OrganizationId == _currentTenant.OrganizationId);

            entity
                .HasOne<Organization>()
                .WithMany()
                .HasForeignKey(i => i.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ApplicationRole>(entity =>
        {
            entity.Property(r => r.Name).HasMaxLength(64);
            entity.Property(r => r.NormalizedName).HasMaxLength(64);
        });

        builder.Entity<SeedRun>(entity =>
        {
            entity.Property(s => s.Key).HasMaxLength(128).IsRequired();
            entity.Property(s => s.AppliedAtUtc).IsRequired();
            entity.HasIndex(s => s.Key).IsUnique();
        });

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var isDeletedProperty = Expression.Call(
                typeof(EF),
                nameof(EF.Property),
                [typeof(bool)],
                parameter,
                Expression.Constant(nameof(ISoftDeletable.IsDeleted)));
            var softDeleteFilter = Expression.Equal(isDeletedProperty, Expression.Constant(false));

#pragma warning disable CS0618 // GetQueryFilter is still needed for combining existing model-level filter lambdas.
            var existingFilter = entityType.GetQueryFilter();
#pragma warning restore CS0618
            Expression combinedBody = softDeleteFilter;
            if (existingFilter is not null)
            {
                var replacedBody = new ReplaceParameterVisitor(existingFilter.Parameters[0], parameter)
                    .Visit(existingFilter.Body)!;
                combinedBody = Expression.AndAlso(replacedBody, softDeleteFilter);
            }

            builder.Entity(entityType.ClrType)
                .HasQueryFilter(Expression.Lambda(combinedBody, parameter));
        }
    }

    private sealed class ReplaceParameterVisitor(ParameterExpression source, ParameterExpression target)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == source ? target : base.VisitParameter(node);
    }
}





