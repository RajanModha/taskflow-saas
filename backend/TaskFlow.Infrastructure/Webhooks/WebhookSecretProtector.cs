using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace TaskFlow.Infrastructure.Webhooks;

/// <summary>Protects webhook signing secrets at rest (reversible for outbound HMAC signing).</summary>
public sealed class WebhookSecretProtector(IDataProtectionProvider dataProtectionProvider)
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("TaskFlow.Webhooks.Secret.v1");

    public string Protect(string plainSecret)
    {
        var bytes = Encoding.UTF8.GetBytes(plainSecret);
        var protectedBytes = _protector.Protect(bytes);
        return Convert.ToBase64String(protectedBytes);
    }

    public string Unprotect(string stored)
    {
        var protectedBytes = Convert.FromBase64String(stored);
        var bytes = _protector.Unprotect(protectedBytes);
        return Encoding.UTF8.GetString(bytes);
    }
}
