using Cg.ProjectName.Application.Demos.DemoEmployees.Base;
using Cg.ProjectName.Application.Interfaces.Shared;
using Cg.ProjectName.Domain.Enums.Shared;

namespace Cg.ProjectName.Application.Demos.DemoEmployees.Commands.UpdateDemoEmployee;

public class UpdateDemoEmployeeCommand :
    DemoEmployeeCommandBase,
    ICommand<UpdateDemoEmployeeDto>
{
    public required Guid Id { get; set; }

    public required EStatus Status { get; set; }

    /// <summary>
    /// Token de concorrência otimista obtido de uma leitura anterior (GET) do
    /// mesmo recurso. Quando informado, o Update só é aplicado se ainda
    /// corresponder ao valor atual no banco — caso contrário, uma
    /// DbUpdateConcurrencyException é lançada e traduzida em 409 Conflict
    /// pelo ValidationExceptionMiddleware. Opcional para não quebrar
    /// integrações existentes que ainda não enviam esse campo, mas
    /// fortemente recomendado: sem ele, duas edições concorrentes resultam
    /// em last-writer-wins silencioso.
    /// </summary>
    public byte[]? RowVersion { get; set; }
}