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
            entity.HasOne<Organization>()
                .WithMany()
                .HasForeignKey(u => u.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Tenant isolation: all org-scoped entities should be filtered by org_id.
            entity.HasQueryFilter(u => !_currentTenant.IsSet || u.OrganizationId == _currentTenant.OrganizationId);
        });

        builder.Entity<ApplicationRole>(entity =>
        {
            entity.Property(r => r.Name).HasMaxLength(64);
            entity.Property(r => r.NormalizedName).HasMaxLength(64);
        });
    }
}
