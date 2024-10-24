
namespace DynamicTokens.API.Endpoints;

public static class EndpointRegistrationExtensions
{
    public static void AddMinimalAPIEndpoints(this IServiceCollection services)
    {
        var types = typeof(Program).Assembly.GetTypes()
            .Where(t => !t.IsInterface && t.IsAssignableTo(typeof(IEndpoint)));
        foreach(var type in types)
            services.AddScoped(typeof(IEndpoint), type);            
    }

    public static void UseMinimalAPIEndpoints(this WebApplication app)
    {
        var services = app.Services.CreateScope().ServiceProvider.GetServices<IEndpoint>();
        foreach (var service in services)
            service.Register(app);
    }
}