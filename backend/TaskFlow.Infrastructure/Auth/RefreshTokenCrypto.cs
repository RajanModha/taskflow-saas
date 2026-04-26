using System.Security.Cryptography;
using System.Text;

namespace TaskFlow.Infrastructure.Auth;

internal static class RefreshTokenCrypto
{
    /// <summary>Generates a raw refresh token and its SHA-256 hex hash (UTF-8 bytes of raw).</summary>
    public static (string Raw, string Hash) GenerateToken()
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        return (raw, hash);
    }

    public static string HashRaw(string raw) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
}
