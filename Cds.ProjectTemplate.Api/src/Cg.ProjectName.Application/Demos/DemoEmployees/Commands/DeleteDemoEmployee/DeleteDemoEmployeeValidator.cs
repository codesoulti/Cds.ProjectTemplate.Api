using FluentValidation;

namespace Cg.ProjectName.Application.Demos.DemoEmployees.Commands.DeleteDemoEmployee;

public class DeleteDemoEmployeeValidator : AbstractValidator<DeleteDemoEmployeeCommand>
{
    public DeleteDemoEmployeeValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Employee ID is required");
    }
}
