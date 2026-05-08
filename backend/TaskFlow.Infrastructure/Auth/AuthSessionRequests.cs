using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Auth;
using TaskFlow.Infrastructure.Identity;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Auth;

public sealed class GetProfileQueryHandler(
    TaskFlowDbContext dbContext,
    UserManager<ApplicationUser> userManager) : IRequestHandler<GetProfileQuery, UserProfileResponse?>
{
    public async Task<UserProfileResponse?> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        return await AuthRequestCommon.MapToProfileResponseAsync(dbContext, userManager, user, cancellationToken);
    }
}
