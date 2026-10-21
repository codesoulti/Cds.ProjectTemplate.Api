using Cg.ProjectName.Domain.Interfaces.Repositories.Demos.DemoEmployees;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cg.ProjectName.Application.Demos.DemoEmployees.Jobs;

public sealed class DemoEmployeeMaintenanceJob(
    IDemoEmployeeRepository demoEmployeeRepository,
    ILogger<DemoEmployeeMaintenanceJob> logger)
    : IDemoEmployeeMaintenanceJob
{
    public async Task ExecuteAsync()
    {
        logger.LogInformation(
            "Iniciando manutenção automática de funcionários em {ExecutedAt}.",
            DateTime.UtcNow);

        // Coloque aqui a chamada ao caso de uso responsável pela manutenção.
        // Exemplo: localizar funcionários desligados e inativá-los.
        var employees = await demoEmployeeRepository.Query()
            .Where(w => w.DateTermination != null && w.DateTermination <= DateTime.UtcNow)
            .ToListAsync();

        foreach (var item in employees)
        {
            item.Inactivate();
        }

        logger.LogInformation(
            "Manutenção automática de funcionários concluída em {ExecutedAt}.",
            DateTime.UtcNow);
    }
}