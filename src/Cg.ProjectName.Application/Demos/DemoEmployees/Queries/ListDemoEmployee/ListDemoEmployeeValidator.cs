using FluentValidation;

namespace Cg.ProjectName.Application.Demos.DemoEmployees.Queries.ListDemoEmployee;

// Único command/query da feature sem validator. Sem isso, CurrentPage <= 0
// vira OFFSET negativo e PageSize <= 0 vira FETCH NEXT negativo — o SQL
// Server rejeita ambos com SqlException, que não é um DbUpdateException e
// por isso cai no handler de erro genérico (500) em vez de um 400 claro.
// PageSize também precisa de um teto: sem um, "PageSize=2000000000" é aceito
// e tenta materializar um result set sem limite prático.
public class ListDemoEmployeeValidator : AbstractValidator<ListDemoEmployeeCommand>
{
    private const int MaxPageSize = 100;

    public ListDemoEmployeeValidator()
    {
        RuleFor(x => x.CurrentPage)
            .GreaterThanOrEqualTo(1)
            .WithMessage("CurrentPage must be greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize)
            .WithMessage($"PageSize must be between 1 and {MaxPageSize}.");
    }
}
