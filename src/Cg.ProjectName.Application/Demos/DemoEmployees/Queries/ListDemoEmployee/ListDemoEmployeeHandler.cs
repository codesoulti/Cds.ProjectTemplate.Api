using AutoMapper;
using Cg.ProjectName.Application.Shared.Paginations.Dapper;
using Cg.ProjectName.Domain.Interfaces.Repositories.Demos.DemoEmployees;
using MediatR;

namespace Cg.ProjectName.Application.Demos.DemoEmployees.Queries.ListDemoEmployee;

public class ListDemoEmployeeHandler(
    IDemoEmployeeReadRepository demoEmployeeRepository,
    IMapper mapper)
        : IRequestHandler<ListDemoEmployeeCommand, DapperPaginatedListDto<ListDemoEmployeeDto>>
{
    private readonly IDemoEmployeeReadRepository _repository = demoEmployeeRepository;
    private readonly IMapper _mapper = mapper;

    public async Task<DapperPaginatedListDto<ListDemoEmployeeDto>> Handle(ListDemoEmployeeCommand command, CancellationToken cancellationToken)
    {
        var query = await _repository.GetListAsync(
            command.Name,
            command.OfficeId,
            command.Status,
            command.CurrentPage,
            command.PageSize,
            command.Sorting,
            cancellationToken);

        var employyes = _mapper.Map<DapperPaginatedListDto<ListDemoEmployeeDto>>(query);

        return employyes;
    }
}