using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Auth;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Auth;
using TaskFlow.Infrastructure.Identity;

namespace TaskFlow.Infrastructure.Features.Auth.Handlers;

public sealed class UpdateProfileCommandHandler(
    IAuthRepository authRepository,
    UserManager<ApplicationUser> userManager,
    ILogger<UpdateProfileCommandHandler> logger) : IRequestHandler<UpdateProfileCommand, UpdateProfileOutcome>
{
    public async Task<UpdateProfileOutcome> Handle(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        var userId = command.UserId;
        var request = command.Request;
        var user = await userManager.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return new UpdateProfileServerError();
        }

        if (!string.IsNullOrWhiteSpace(request.UserName))
        {
            var trimmedName = request.UserName.Trim();
            var normalized = trimmedName.ToUpperInvariant();
            if (normalized != user.NormalizedUserName)
            {
                var takenInOrg = await authRepository.UserNameTakenInOrganizationAsync(
                    user.OrganizationId,
                    userId,
                    normalized,
                    cancellationToken);
                if (takenInOrg)
                {
                    return new UpdateProfileUserNameConflict();
                }

                var setName = await userManager.SetUserNameAsync(user, trimmedName);
                if (!setName.Succeeded)
                {
                    if (setName.Errors.Any(e => e.Code == "DuplicateUserName"))
                    {
                        return new UpdateProfileUserNameConflict();
                    }

                    logger.LogWarning(
                        "SetUserNameAsync failed for user {UserId}: {Errors}",
                        user.Id,
                        string.Join("; ", setName.Errors.Select(e => $"{e.Code}: {e.Description}")));
                    return new UpdateProfileServerError();
                }
            }
        }

        if (request.DisplayName is not null)
        {
            var trimmedDisplay = request.DisplayName.Trim();
            user.DisplayName = trimmedDisplay.Length == 0 ? null : trimmedDisplay;
            var updateDisplay = await userManager.UpdateAsync(user);
            if (!updateDisplay.Succeeded)
            {
                logger.LogWarning(
                    "UpdateAsync failed after display name change for user {UserId}: {Errors}",
                    user.Id,
                    string.Join("; ", updateDisplay.Errors.Select(e => $"{e.Code}: {e.Description}")));
                return new UpdateProfileServerError();
            }
        }

        var refreshed = await userManager.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (refreshed is null)
        {
            return new UpdateProfileServerError();
        }
        var profile = await AuthRequestCommon.MapToProfileResponseAsync(authRepository, userManager, refreshed, cancellationToken);
        return new UpdateProfileSucceeded(profile);
    }
}
