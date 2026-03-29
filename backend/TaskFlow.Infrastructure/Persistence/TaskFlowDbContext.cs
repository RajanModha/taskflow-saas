using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Infrastructure.Identity;

namespace TaskFlow.Infrastructure.Persistence;

public sealed class TaskFlowDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public TaskFlowDbContext(DbContextOptions<TaskFlowDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.UserName).HasMaxLength(64);
            entity.Property(u => u.NormalizedUserName).HasMaxLength(64);
            entity.Property(u => u.Email).HasMaxLength(256);
            entity.Property(u => u.NormalizedEmail).HasMaxLength(256);
            entity.Property(u => u.CreatedAtUtc).IsRequired();
        });

        builder.Entity<ApplicationRole>(entity =>
        {
            entity.Property(r => r.Name).HasMaxLength(64);
            entity.Property(r => r.NormalizedName).HasMaxLength(64);
        });
    }
}
