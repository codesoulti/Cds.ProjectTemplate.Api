namespace Cg.ProjectName.WebApi.Consts;

public static class WebConsts
{
    public const string SwaggerUiEndPoint = "/swagger";
    public const bool SwaggerUiEnabled = true;

    // O toggle/endpoint do dashboard do Hangfire agora é lido de appsettings
    // ("Hangfire:Enabled" / "Hangfire:DashboardPath") em HangfireConfiguration,
    // em vez de uma constante fixa aqui — permite ligar/desligar por ambiente.
}