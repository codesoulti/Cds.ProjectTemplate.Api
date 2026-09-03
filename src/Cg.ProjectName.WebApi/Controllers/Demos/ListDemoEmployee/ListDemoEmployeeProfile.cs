using AutoMapper;
using Cg.ProjectName.Application.Demos.DemoEmployees.Queries.ListDemoEmployee;

namespace Cg.ProjectName.WebApi.Controllers.Demos.ListDemoEmployee;

/// <summary>
/// Profile for mapping ListDemoEmployee feature requests to commands
/// </summary>
public class ListDemoEmployeeProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for the ListDemoEmployee feature
    /// </summary>
    public ListDemoEmployeeProfile()
    {
        // O CreateMap<PagedAndSortedRequest, PagedAndSortedCommand> foi
        // removido: PagedAndSortedCommand não tinha nenhum consumidor no
        // sistema (o fluxo real usa ListDemoEmployeeRequest/Command, ambos
        // com SortingOptions?) e, além de morto, o mapeamento era incompatível
        // — Sorting era SortingOptions? de um lado e string do outro —
        // lançaria AutoMapperMappingException se algum dia fosse invocado.
        CreateMap<ListDemoEmployeeRequest, ListDemoEmployeeCommand>();
    }
}
