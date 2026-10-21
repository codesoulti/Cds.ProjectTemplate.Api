namespace Cg.ProjectName.Application.Demos.DemoEmployees.Commands.UpdateDemoEmployee;

public class UpdateDemoEmployeeDto
{
    public Guid Id { get; set; }

    // Novo valor do token de concorrência após o UPDATE — o cliente deve
    // guardar este valor e reenviá-lo no próximo PUT (UpdateDemoEmployeeCommand.RowVersion).
    public byte[] RowVersion { get; set; } = [];
}