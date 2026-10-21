using AutoMapper;
using Cg.ProjectName.Domain.Entities.Demos;

namespace Cg.ProjectName.Application.Demos.DemoEmployees.Commands.UpdateDemoEmployee;

public class UpdateDemoEmployeeProfile : Profile
{
    public UpdateDemoEmployeeProfile()
    {
        // CreateMap<UpdateDemoEmployeeCommand, DemoEmployee> foi removido:
        // nenhum consumidor usa esse mapa (UpdateDemoEmployeeHandler aplica
        // as mudanças via DemoEmployee.Change/Activate/Inactivate de
        // propósito, para preservar encapsulamento) e, além de morto, o mapa
        // era incompleto/perigoso — OfficeName (string) nunca resolveria
        // para OfficeId (Guid), e um Map<DemoEmployee> por engano
        // sobrescreveria Id/CreatedAt/IsDeleted da entidade rastreada.
        CreateMap<DemoEmployee, UpdateDemoEmployeeDto>();
    }
} 