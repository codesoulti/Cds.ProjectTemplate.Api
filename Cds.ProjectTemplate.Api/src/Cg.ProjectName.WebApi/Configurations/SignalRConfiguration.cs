namespace Cg.ProjectName.WebApi.Configurations;

public static class SignalRConfiguration
{
    public static IServiceCollection AddSignalRConfiguration(
        this IServiceCollection services)
    {
        //services.AddScoped<ICartNotifier, SignalRCartNotifier>();
        //services.AddScoped<IPaymentNotifier, SignalRPaymentNotifier>();
        services.AddSignalR();

        return services;
    }

    public static IEndpointRouteBuilder MapSignalRHubs(
        this IEndpointRouteBuilder endpoints)
    {
        //endpoints.MapHub<PaymentHub>("/hubs/payments");
        //endpoints.MapHub<CartHub>("/hubs/Carts");

        return endpoints;
    }
}