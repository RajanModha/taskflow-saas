using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TaskFlow.API.Extensions;

public static class ControllerValidationExtensions
{
    public static ActionResult ValidationProblemFromErrors(
        this ControllerBase controller,
        IReadOnlyDictionary<string, string[]> errors)
    {
        var modelState = new ModelStateDictionary();
        foreach (var kvp in errors)
        {
            foreach (var message in kvp.Value)
            {
                modelState.AddModelError(kvp.Key, message);
            }
        }

        return controller.ValidationProblem(modelState);
    }
}
