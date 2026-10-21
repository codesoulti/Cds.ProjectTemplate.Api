using FluentValidation;

namespace Cg.ProjectName.Domain.Validations;

public class PasswordValidator : AbstractValidator<string>
{
    public PasswordValidator()
    {
        RuleFor(password => password)
            .NotEmpty()
            .MinimumLength(8)
            // Limite superior defensivo: evita aceitar entradas
            // arbitrariamente grandes antes de aplicar as regexes abaixo.
            .MaximumLength(128)
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one number.")
            .Matches(@"[\!\?\*\.\@\#\$\%\^\&\+\=]").WithMessage("Password must contain at least one special character.");
    }
}