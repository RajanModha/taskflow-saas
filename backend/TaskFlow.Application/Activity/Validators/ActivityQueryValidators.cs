using FluentValidation;

namespace TaskFlow.Application.Activity.Validators;

public sealed class GetTaskActivityQueryValidator : AbstractValidator<GetTaskActivityQuery>
{
    public GetTaskActivityQueryValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class GetProjectActivityQueryValidator : AbstractValidator<GetProjectActivityQuery>
{
    public GetProjectActivityQueryValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
