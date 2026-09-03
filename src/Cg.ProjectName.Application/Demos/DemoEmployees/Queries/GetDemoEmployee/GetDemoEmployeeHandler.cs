using AutoMapper;
using Cg.ProjectName.Domain.Entities.Demos;
using Cg.ProjectName.Domain.Exceptions;
using Cg.ProjectName.Domain.Interfaces.Repositories.Demos.DemoEmployees;
using MediatR;

namespace Cg.ProjectName.Application.Demos.DemoEmployees.Queries.GetDemoEmployee;

public class GetDemoEmployeeHandler(
    IDemoEmployeeReadRepository demoEmployeeRepository,
    IMapper mapper) 
        : IRequestHandler<GetDemoEmployeeCommand, GetDemoEmployeeDto>
{
    private readonly IDemoEmployeeReadRepository _repository = demoEmployeeRepository;
    private readonly IMapper _mapper = mapper;

    public async Task<GetDemoEmployeeDto> Handle(GetDemoEmployeeCommand command, CancellationToken cancellationToken)
    {
        // GetDetailByIdAsync (LEFT JOIN com DemoOffices) em vez do GetByIdAsync
        // genérico: este último é de propósito uma consulta de tabela única e
        // não devolveria OfficeName — o mesmo motivo pelo qual GetListAsync
        // também usa SQL escrito à mão em vez do builder genérico.
        var employee = await _repository.GetDetailByIdAsync(command.Id, cancellationToken)
            ?? throw NotFoundException.For<DemoEmployee>(command.Id);

        return _mapper.Map<GetDemoEmployeeDto>(employee);
    }
} 
