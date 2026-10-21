using AutoMapper;
using Cg.ProjectName.Domain.Entities.Demos;

namespace Cg.ProjectName.Application.Demos.DemoEmployees.Commands.CreateDemoEmployee;

public class CreateDemoEmployeeProfile : Profile
{
    public CreateDemoEmployeeProfile()
    {
        // CreateMap<CreateDemoEmployeeCommand, DemoEmployee> foi removido:
        // nunca era usado (o handler cria a entidade via DemoEmployee.Create,
        // não via AutoMapper) e, se algum dia fosse invocado, geraria uma
        // entidade incompleta — o Command carrega OfficeName, não OfficeId,
        // que só é resolvido dentro do handler.
        CreateMap<DemoEmployee, CreateDemoEmployeeDto>();
    }
}