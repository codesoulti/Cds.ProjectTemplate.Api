using Cg.ProjectName.Application.Demos.DemoEmployees.Jobs;
using Hangfire;

namespace Cg.ProjectName.Worker.Configurations;

public static class HangfireConfigurationJobs
{
    public static void RegisterRecurringJobs(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var recurringJobs =
            scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

        recurringJobs.AddOrUpdate<IDemoEmployeeMaintenanceJob>(
            "demo-employee-maintenance",
            job => job.ExecuteAsync(),
            Cron.Daily(2));
    }
}