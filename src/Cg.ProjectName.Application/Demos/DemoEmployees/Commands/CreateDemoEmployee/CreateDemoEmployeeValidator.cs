using FluentValidation;

namespace Cg.ProjectName.Application.Demos.DemoEmployees.Commands.CreateDemoEmployee;

public class CreateDemoEmployeeValidator : AbstractValidator<CreateDemoEmployeeCommand>
{
    public CreateDemoEmployeeValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(200)
            .WithMessage("Name cannot exceed 200 characters.");

        RuleFor(x => x.Document)
            .NotEmpty()
            .WithMessage("Document is required.")
            // Precisa bater com HasMaxLength(20) em DemoEmployeeConfiguration
            // — sem isso, um Document maior passa na validação e só falha no
            // INSERT, virando um 500 genérico via ValidationExceptionMiddleware
            // (DbUpdateException) em vez de um 400 claro.
            .MaximumLength(20)
            .WithMessage("Document cannot exceed 20 characters.");

        RuleFor(x => x.DateHire)
            .NotEmpty()
            .WithMessage("Date of hire is required.");

        RuleFor(x => x.Salary)
            .GreaterThan(0)
            .WithMessage("Salary must be greater than 0.");

        RuleFor(x => x.OfficeName)
            .NotEmpty()
            .WithMessage("Office name is required.")
            // Precisa bater com HasMaxLength(200) em DemoOfficeConfiguration.
            .MaximumLength(200)
            .WithMessage("Office name cannot exceed 200 characters.");
    }
}
