using System.Net;
using System.Net.Sockets;

namespace TaskFlow.Infrastructure.Webhooks;

/// <summary>HTTPS-only webhook URLs with basic SSRF hardening (literal IPs and localhost names).</summary>
public static class WebhookUrlValidator
{
    public static string? Validate(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return "Url is required.";
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return "Url must be a valid absolute URL.";
        }

        if (uri.Scheme != Uri.UriSchemeHttps)
        {
            return "Url must use HTTPS.";
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            return "Url must not contain user credentials.";
        }

        var host = uri.IdnHost;
        if (host.Length == 0)
        {
            return "Url must have a valid host.";
        }

        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(host, "0.0.0.0", StringComparison.OrdinalIgnoreCase))
        {
            return "Url must not target loopback or non-routable hosts.";
        }

        if (IPAddress.TryParse(host, out var ip))
        {
            if (IsBlockedEndpoint(ip))
            {
                return "Url must not target loopback, link-local, or private networks.";
            }
        }

        return null;
    }

    public static bool TryCreateValidatedUri(string url, out Uri? uri, out string? error)
    {
        error = Validate(url);
        if (error is not null)
        {
            uri = null;
            return false;
        }

        uri = new Uri(url.Trim(), UriKind.Absolute);
        return true;
    }

    private static bool IsBlockedEndpoint(IPAddress ip)
    {
        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (IPAddress.IPv6Loopback.Equals(ip))
            {
                return true;
            }

            if (ip.IsIPv6LinkLocal)
            {
                return true;
            }

            if (IsIpv6UniqueLocal(ip))
            {
                return true;
            }

            if (ip.IsIPv4MappedToIPv6)
            {
                return IsBlockedIpv4(ip.MapToIPv4());
            }

            return false;
        }

        return IsBlockedIpv4(ip);
    }

    private static bool IsIpv6UniqueLocal(IPAddress ip)
    {
        var bytes = ip.GetAddressBytes();
        return bytes.Length >= 1 && (bytes[0] & 0xfe) == 0xfc;
    }

    private static bool IsBlockedIpv4(IPAddress ip)
    {
        var bytes = ip.GetAddressBytes();
        if (bytes.Length != 4)
        {
            return true;
        }

        if (IPAddress.IsLoopback(ip))
        {
            return true;
        }

        if (bytes[0] == 10)
        {
            return true;
        }

        if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
        {
            return true;
        }

        if (bytes[0] == 192 && bytes[1] == 168)
        {
            return true;
        }

        if (bytes[0] == 169 && bytes[1] == 254)
        {
            return true;
        }

        if (bytes[0] == 127)
        {
            return true;
        }

        if (bytes[0] == 0)
        {
            return true;
        }

        if (bytes[0] >= 224)
        {
            return true;
        }

        return false;
    }
}
