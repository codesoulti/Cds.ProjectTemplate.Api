using Cg.ProjectName.Infrastructure.CrossCutting.Ioc.Configurations;
using Cg.ProjectName.Worker.Configurations;
using Hangfire;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddHangfireConfiguration(builder.Configuration)
    .AddHangfireServer();

var host = builder.Build();

HangfireConfigurationJobs.RegisterRecurringJobs(host.Services);

await host.RunAsync();