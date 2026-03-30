namespace TaskFlow.Application.Common;

public sealed class TenantContextMissingException : Exception
{
    public TenantContextMissingException()
        : base("Tenant context is missing for this request.")
    {
    }
}

