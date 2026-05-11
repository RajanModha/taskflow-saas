using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TaskFlow.Application.Auth;

namespace TaskFlow.API.Extensions;

public static class AuthControllerExtensions
{
    public static ActionResult NewPasswordSameAsCurrentValidationProblem(this ControllerBase controller)
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError(
            nameof(ChangePasswordRequest.NewPassword),
            "New password must be different from your current password.");
        return controller.ValidationProblem(modelState);
    }

    public static ActionResult SamePasswordValidationProblem(this ControllerBase controller)
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError(
            nameof(ResetPasswordRequest.NewPassword),
            "Your new password must be different from your current password.");
        return controller.ValidationProblem(modelState);
    }

}
