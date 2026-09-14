using AutoMapper;
using Cg.ProjectName.Domain.Interfaces.Repositories.Demos.DemoEmployees;
using Cg.ProjectName.Domain.ValueObjects.Paginations;
using MediatR;

namespace Cg.ProjectName.Application.Demos.DemoEmployees.Queries.ListDemoEmployee;

public class ListDemoEmployeeHandler(
    IDemoEmployeeReadRepository demoEmployeeRepository,
    IMapper mapper)
        : IRequestHandler<ListDemoEmployeeCommand, PaginatedListResult<ListDemoEmployeeDto>>
{
    private readonly IDemoEmployeeReadRepository _repository = demoEmployeeRepository;
    private readonly IMapper _mapper = mapper;

    public async Task<PaginatedListResult<ListDemoEmployeeDto>> Handle(ListDemoEmployeeCommand command, CancellationToken cancellationToken)
    {
        var query = await _repository.GetListPagedAsync(
            command.Name,
            command.OfficeId,
            command.Status,
            command.CurrentPage,
            command.PageSize,
            command.Sorting,
            cancellationToken);

        var employyes = _mapper.Map<PaginatedListResult<ListDemoEmployeeDto>>(query);

        return employyes;
    }
}