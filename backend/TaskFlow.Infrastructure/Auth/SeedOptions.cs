namespace TaskFlow.Infrastructure.Auth;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>
    /// When set together with <see cref="AdminPassword"/>, ensures an Admin user exists (typically Development only).
    /// </summary>
    public string? AdminEmail { get; init; }

    public string? AdminPassword { get; init; }
}
