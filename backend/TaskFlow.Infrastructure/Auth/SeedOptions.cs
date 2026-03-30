namespace TaskFlow.Infrastructure.Auth;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>
    /// When set together with <see cref="AdminPassword"/>, ensures an Admin user exists (typically Development only).
    /// </summary>
    public string? AdminEmail { get; init; }

    public string? AdminPassword { get; init; }

    /// <summary>
    /// Enables bulk demo data generation (users, organizations, projects, tasks).
    /// Intended for demos and local environments, not production customer data.
    /// </summary>
    public bool DemoDataEnabled { get; init; } = false;

    public int DemoOrganizationsCount { get; init; } = 12;
    public int DemoUsersPerOrganization { get; init; } = 5;
    public int DemoProjectsPerOrganization { get; init; } = 6;
    public int DemoTasksPerProject { get; init; } = 8;
    public string DemoUserPassword { get; init; } = "Demo123!";
}
