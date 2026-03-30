using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Auth;
using TaskFlow.Domain.Common;
using TaskFlow.Infrastructure.Identity;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Auth;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    TaskFlowDbContext dbContext,
    IJwtTokenGenerator tokenGenerator,
    TimeProvider timeProvider) : IAuthService
{
    public async Task<RegisterOutcome> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var orgId = Guid.NewGuid();
        var organization = new Organization
        {
            Id = orgId,
            Name = request.OrganizationName,
            JoinCode = GenerateJoinCode(),
            CreatedAtUtc = now,
        };

        await dbContext.Organizations.AddAsync(organization, cancellationToken);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.UserName,
            Email = request.Email,
            EmailConfirmed = false,
            CreatedAtUtc = now,
            OrganizationId = orgId,
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return ToRegisterFailure(createResult);
        }

        var roleResult = await userManager.AddToRoleAsync(user, DomainRoles.User);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return ToRegisterFailure(roleResult);
        }

        var roles = await userManager.GetRolesAsync(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        var token = tokenGenerator.CreateAccessToken(user.Id, user.Email ?? string.Empty, roles, organization.Id, now, out var expires);
        var response = new AuthResponse(token, new DateTimeOffset(expires, TimeSpan.Zero), "Bearer");
        return new RegisterSucceeded(response);
    }

    public async Task<LoginOutcome> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        if (user is null)
        {
            return new LoginFailed("Invalid email or password.");
        }

        var valid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!valid)
        {
            return new LoginFailed("Invalid email or password.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var roles = await userManager.GetRolesAsync(user);
        if (user.OrganizationId == Guid.Empty)
        {
            return new LoginFailed("User has not been assigned to a workspace.");
        }

        var token = tokenGenerator.CreateAccessToken(user.Id, user.Email ?? string.Empty, roles, user.OrganizationId, now, out var expires);
        return new LoginSucceeded(new AuthResponse(token, new DateTimeOffset(expires, TimeSpan.Zero), "Bearer"));
    }

    public async Task<UserProfileResponse?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return null;
        }

        if (user.OrganizationId == Guid.Empty)
        {
            return new UserProfileResponse(
                user.Id,
                user.Email ?? string.Empty,
                user.UserName ?? string.Empty,
                Array.Empty<string>(),
                Guid.Empty,
                string.Empty,
                string.Empty);
        }

        var roles = await userManager.GetRolesAsync(user);
        var organization = await dbContext.Organizations.FirstOrDefaultAsync(
            o => o.Id == user.OrganizationId,
            cancellationToken);

        return new UserProfileResponse(
            user.Id,
            user.Email ?? string.Empty,
            user.UserName ?? string.Empty,
            roles.ToArray(),
            user.OrganizationId,
            organization?.Name ?? string.Empty,
            organization?.JoinCode ?? string.Empty);
    }

    private static string GenerateJoinCode()
    {
        // Non-cryptographic is fine for a join code in dev; production can switch to stronger RNG.
        // Format: 8 alphanumeric chars.
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

    private static RegisterFailed ToRegisterFailure(IdentityResult result)
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
}
