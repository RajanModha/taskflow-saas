using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using TaskFlow.Application.Auth;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Identity;

namespace TaskFlow.Infrastructure.Auth;

internal static class AuthRequestCommon
{
    public static async Task<UserProfileResponse> MapToProfileResponseAsync(
        IAuthRepository authRepository,
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        if (user.OrganizationId == Guid.Empty)
        {
            var emptyRoles = Array.Empty<string>();
            return new UserProfileResponse(
                user.Id,
                user.Email ?? string.Empty,
                user.UserName ?? string.Empty,
                emptyRoles,
                PickPrimaryRole(emptyRoles),
                Guid.Empty,
                string.Empty,
                string.Empty,
                user.DisplayName,
                user.AvatarUrl,
                new DateTimeOffset(user.CreatedAtUtc, TimeSpan.Zero));
        }

        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        var organization = await authRepository.GetOrganizationByIdAsync(user.OrganizationId, cancellationToken);

        return new UserProfileResponse(
            user.Id,
            user.Email ?? string.Empty,
            user.UserName ?? string.Empty,
            roles,
            PickPrimaryRole(roles),
            user.OrganizationId,
            organization?.Name ?? string.Empty,
            organization?.JoinCode ?? string.Empty,
            user.DisplayName,
            user.AvatarUrl,
            new DateTimeOffset(user.CreatedAtUtc, TimeSpan.Zero));
    }

    public static string PickPrimaryRole(IReadOnlyList<string> roles)
    {
        foreach (var role in roles)
        {
            if (string.Equals(role, DomainRoles.Admin, StringComparison.OrdinalIgnoreCase))
            {
                return DomainRoles.Admin;
            }
        }

        return roles.Count > 0 ? roles[0] : DomainRoles.User;
    }

    public static async Task<IdentityResult> ValidatePasswordAgainstIdentityAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        string password,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var errors = new List<IdentityError>();
        foreach (var validator in userManager.PasswordValidators)
        {
            var result = await validator.ValidateAsync(userManager, user, password);
            if (!result.Succeeded)
            {
                errors.AddRange(result.Errors);
            }
        }

        return errors.Count == 0
            ? IdentityResult.Success
            : IdentityResult.Failed(errors.ToArray());
    }

    public static IReadOnlyDictionary<string, string[]> MapIdentityPasswordErrors(IdentityResult result)
    {
        var messages = result.Errors.Select(e => e.Description).ToArray();
        return new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(ResetPasswordRequest.NewPassword)] = messages,
        };
    }

    public static IReadOnlyDictionary<string, string[]> MapChangePasswordIdentityErrors(IdentityResult result)
    {
        var messages = result.Errors.Select(e => e.Description).ToArray();
        return new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(ChangePasswordRequest.NewPassword)] = messages,
        };
    }

    public static string HashVerificationToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    public static bool TokenHashesEqual(string? storedHex, string computedHex)
    {
        if (storedHex is null || storedHex.Length != computedHex.Length)
        {
            return false;
        }

        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(storedHex),
                Convert.FromHexString(computedHex));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static string GenerateJoinCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = new byte[8];
        Random.Shared.NextBytes(bytes);
        var chars = new char[8];

        for (var i = 0; i < 8; i++)
        {
            chars[i] = alphabet[bytes[i] % alphabet.Length];
        }

        return new string(chars);
    }

    public static RegisterFailed ToRegisterFailure(IdentityResult result)
    {
        var dict = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var error in result.Errors)
        {
            var key = error.Code switch
            {
                "DuplicateUserName" => nameof(RegisterRequest.UserName),
                "DuplicateEmail" => nameof(RegisterRequest.Email),
                "InvalidEmail" => nameof(RegisterRequest.Email),
                "PasswordTooShort" => nameof(RegisterRequest.Password),
                "PasswordRequiresDigit" => nameof(RegisterRequest.Password),
                "PasswordRequiresLower" => nameof(RegisterRequest.Password),
                "PasswordRequiresUpper" => nameof(RegisterRequest.Password),
                "PasswordRequiresNonAlphanumeric" => nameof(RegisterRequest.Password),
                _ => string.Empty,
            };

            if (string.IsNullOrEmpty(key))
            {
                key = "general";
            }

            if (!dict.TryGetValue(key, out var list))
            {
                list = [];
                dict[key] = list;
            }

            list.Add(error.Description);
        }

        return new RegisterFailed(dict.ToDictionary(k => k.Key, v => v.Value.ToArray()));
    }

    public static SessionConnectionInfo? GetSessionConnectionInfo(IHttpContextAccessor httpContextAccessor)
    {
        var http = httpContextAccessor.HttpContext;
        if (http is null)
        {
            return null;
        }

        var ua = http.Request.Headers.UserAgent.ToString();
        if (string.IsNullOrWhiteSpace(ua))
        {
            ua = null;
        }

        var ip = http.Connection.RemoteIpAddress?.ToString();
        return new SessionConnectionInfo(ua, ip);
    }
}
