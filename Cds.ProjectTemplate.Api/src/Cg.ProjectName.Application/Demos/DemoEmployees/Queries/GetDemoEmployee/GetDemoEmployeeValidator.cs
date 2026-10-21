using FluentValidation;

namespace Cg.ProjectName.Application.Demos.DemoEmployees.Queries.GetDemoEmployee;

public class GetDemoEmployeeValidator : AbstractValidator<GetDemoEmployeeCommand>
{
    public GetDemoEmployeeValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Demo Employee ID is required");
    }
}
