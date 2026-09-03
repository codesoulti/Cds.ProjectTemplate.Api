using AutoMapper;
using Cg.ProjectName.Domain.Entities.Demos;

namespace Cg.ProjectName.Application.Demos.DemoEmployees.Queries.GetDemoEmployee;

public class GetDemoEmployeeProfile : Profile
{
    public GetDemoEmployeeProfile()
    {
        // O mapa reverso (GetDemoEmployeeDto -> DemoEmployee) foi removido:
        // não tinha nenhum consumidor e, se algum dia fosse invocado por
        // engano, sobrescreveria a entidade rastreada pelo EF Core direto a
        // partir de um DTO de leitura, contornando os métodos de domínio
        // (Change/Activate/Inactivate) que preservam o encapsulamento.
        //
        // A fonte mudou de DemoEmployee para DemoEmployeeListItem:
        // GetDemoEmployeeHandler passou a usar GetDetailByIdAsync (LEFT JOIN
        // com DemoOffices) em vez do GetByIdAsync genérico de tabela única,
        // para que o detalhe de um funcionário também exponha OfficeName —
        // antes só a listagem trazia esse campo.
        CreateMap<DemoEmployeeListItem, GetDemoEmployeeDto>();
    }
} 