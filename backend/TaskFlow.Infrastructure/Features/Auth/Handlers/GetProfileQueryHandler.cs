using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Auth;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Auth;
using TaskFlow.Infrastructure.Identity;

namespace TaskFlow.Infrastructure.Features.Auth.Handlers;

public sealed class GetProfileQueryHandler(
    IAuthRepository authRepository,
    UserManager<ApplicationUser> userManager) : IRequestHandler<GetProfileQuery, UserProfileResponse?>
{
    public async Task<UserProfileResponse?> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await userManager.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        return await AuthRequestCommon.MapToProfileResponseAsync(authRepository, userManager, user, cancellationToken);
    }
}
