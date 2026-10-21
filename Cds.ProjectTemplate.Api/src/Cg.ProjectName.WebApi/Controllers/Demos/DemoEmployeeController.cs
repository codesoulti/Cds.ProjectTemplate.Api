using AutoMapper;
using Cg.ProjectName.Application.Demos.DemoEmployees.Commands.CreateDemoEmployee;
using Cg.ProjectName.Application.Demos.DemoEmployees.Commands.DeleteDemoEmployee;
using Cg.ProjectName.Application.Demos.DemoEmployees.Commands.UpdateDemoEmployee;
using Cg.ProjectName.Application.Demos.DemoEmployees.Queries.GetDemoEmployee;
using Cg.ProjectName.Application.Demos.DemoEmployees.Queries.ListDemoEmployee;
using Cg.ProjectName.WebApi.Controllers.Base;
using Cg.ProjectName.WebApi.Controllers.Demos.CreateDemoEmployee;
using Cg.ProjectName.WebApi.Controllers.Demos.DeleteDemoEmployee;
using Cg.ProjectName.WebApi.Controllers.Demos.GetDemoEmployee;
using Cg.ProjectName.WebApi.Controllers.Demos.ListDemoEmployee;
using Cg.ProjectName.WebApi.Controllers.Demos.UpdateDemoEmployee;
using Cg.ProjectName.WebApi.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Cg.ProjectName.WebApi.Controllers.Demos;

/// <summary>
/// Controller for managing demo employee operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DemoEmployeeController : BaseController
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of DemoEmployeeController
    /// </summary>
    /// <param name="mediator">The mediator instance</param>
    /// <param name="mapper">The AutoMapper instance</param>
    public DemoEmployeeController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    /// <summary>
    /// Retrieves a paginated list of demo employees
    /// </summary>
    /// <param name="request">The filter, sorting and pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The demo employees list if found</returns>
    [HttpGet("List")]
    [ProducesResponseType(typeof(ApiResponseWithData<ListDemoEmployeeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetList([FromQuery] ListDemoEmployeeRequest request, CancellationToken cancellationToken)
    {
        var command = _mapper.Map<ListDemoEmployeeCommand>(request);
        var response = await _mediator.Send(command, cancellationToken);

        return OkPaginated(response);
    }

    /// <summary>
    /// Retrieves a demo employee by their ID
    /// </summary>
    /// <param name="id">The unique identifier of the demo employee</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The demo employee details if found</returns>
    [HttpGet("{id}", Name = nameof(Get))]
    [ProducesResponseType(typeof(ApiResponseWithData<GetDemoEmployeeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new GetDemoEmployeeRequest { Id = id };
        var command = _mapper.Map<GetDemoEmployeeCommand>(request.Id);
        var response = await _mediator.Send(command, cancellationToken);

        return Ok(response, "DemoEmployee retrieved successfully");
    }

    /// <summary>
    /// Creates a new demo employee
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseWithData<CreateDemoEmployeeDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateDemoEmployeeRequest request, CancellationToken cancellationToken)
    {
        // Exceções de negócio/validação são tratadas centralmente por
        // ValidationExceptionMiddleware — o controller fica responsável
        // apenas por orquestrar a chamada, mantendo consistência com os
        // demais endpoints (GetList/Get/Delete).
        var command = _mapper.Map<CreateDemoEmployeeCommand>(request);
        var response = await _mediator.Send(command, cancellationToken);

        // 201 Created com Location apontando para o recurso recém-criado —
        // antes retornava 200 (Ok) apesar da anotação Swagger já dizer
        // Status201Created, um contrato divergente do que a API de fato
        // respondia. BaseController.Created<T> (CreatedAtRoute) existe
        // exatamente para isso.
        return Created(nameof(Get), new { id = response.Id }, response);
    }

    /// <summary>
    /// Updates an existing demo employee
    /// </summary>
    /// <param name="request">The demo employee update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated demo employee details</returns>
    [HttpPut()]
    [ProducesResponseType(typeof(ApiResponseWithData<UpdateDemoEmployeeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    // 409: RowVersion enviado no request não corresponde mais ao valor atual
    // no banco (edição concorrente) — ver DbUpdateConcurrencyException em
    // ValidationExceptionMiddleware e UpdateDemoEmployeeRequest.RowVersion.
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update([FromBody] UpdateDemoEmployeeRequest request, CancellationToken cancellationToken)
    {
        // Exceções de negócio/validação são tratadas centralmente por
        // ValidationExceptionMiddleware — o controller fica responsável
        // apenas por orquestrar a chamada, mantendo consistência com os
        // demais endpoints (GetList/Get/Delete).
        var command = _mapper.Map<UpdateDemoEmployeeCommand>(request);
        var response = await _mediator.Send(command, cancellationToken);

        return Ok(response, "DemoEmployee updated successfully");
    }

    // O endpoint de Activate/Deactivate dedicado foi removido daqui fazia
    // tempo (referenciava ActivateDemoEmployeeCommand/Result, que não
    // existem mais), mas o bloco comentado continuava no arquivo. A
    // transição de Status já é possível via PUT /api/DemoEmployee
    // (UpdateDemoEmployeeCommand.Status -> employee.Activate()/Inactivate()).

    /// <summary>
    /// Deletes a demo employee by their ID
    /// </summary>
    /// <param name="id">The unique identifier of the demo employee to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response if the demo employee was deleted</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new DeleteDemoEmployeeRequest { Id = id };
        var command = _mapper.Map<DeleteDemoEmployeeCommand>(request.Id);
        
        await _mediator.Send(command, cancellationToken);

        return Ok(new ApiResponse
        {
            Success = true,
            Message = "DemoEmployee deleted successfully"
        });
    }
}