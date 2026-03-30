namespace TaskFlow.Infrastructure.Persistence;

public sealed class SeedRun
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public DateTime AppliedAtUtc { get; set; }
}

