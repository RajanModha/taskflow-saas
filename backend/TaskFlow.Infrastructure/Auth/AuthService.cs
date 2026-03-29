using Microsoft.AspNetCore.Identity;
using TaskFlow.Application.Auth;
using TaskFlow.Domain.Common;
using TaskFlow.Infrastructure.Identity;

namespace TaskFlow.Infrastructure.Auth;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    IJwtTokenGenerator tokenGenerator,
    TimeProvider timeProvider) : IAuthService
{
    public async Task<RegisterOutcome> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.UserName,
            Email = request.Email,
            EmailConfirmed = false,
            CreatedAtUtc = now,
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
        var token = tokenGenerator.CreateAccessToken(user.Id, user.Email ?? string.Empty, roles, now, out var expires);
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
        var token = tokenGenerator.CreateAccessToken(user.Id, user.Email ?? string.Empty, roles, now, out var expires);
        return new LoginSucceeded(new AuthResponse(token, new DateTimeOffset(expires, TimeSpan.Zero), "Bearer"));
    }

    public async Task<UserProfileResponse?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return null;
        }

        var roles = await userManager.GetRolesAsync(user);
        return new UserProfileResponse(user.Id, user.Email ?? string.Empty, user.UserName ?? string.Empty, roles.ToArray());
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
