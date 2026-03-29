namespace TaskFlow.Domain.Common;

public static class DomainRoles
{
    public const string Admin = "Admin";
    public const string User = "User";

    public static IReadOnlyList<string> All { get; } = [Admin, User];
}
