using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
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
    public DbSet<SeedRun> SeedRuns => Set<SeedRun>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

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

            entity.Property(p => p.OrganizationId).IsRequired();

            // Fail-closed tenant filter: no tenant context means no data returned.
            entity.HasQueryFilter(p => _currentTenant.IsSet && p.OrganizationId == _currentTenant.OrganizationId);
            entity.HasIndex(p => p.OrganizationId);
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

            entity.HasQueryFilter(t => _currentTenant.IsSet && t.OrganizationId == _currentTenant.OrganizationId);
            entity.HasIndex(t => t.OrganizationId);
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
    }
}
