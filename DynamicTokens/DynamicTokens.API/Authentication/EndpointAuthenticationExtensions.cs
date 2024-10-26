namespace DynamicTokens.API.Authentication;

public static class EndpointAuthenticationExtensions
{
    public static void AddEndpointAuthenticationService(this IServiceCollection service)
    {
        service.AddDistributedMemoryCache();
        service.AddAuthentication().AddBearerToken();
        service.AddCors();
        service.AddSingleton<EndpointAuthentication>();
    }

    public static void UseEndpointAuthentication(this WebApplication app)
    {
        app.UseCors(o => o.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
        TokenService.logger = app.Logger;
    }

    public static RouteHandlerBuilder ApplyEndpointAuthentication(this RouteHandlerBuilder builder, string? roles = null)
    {        
        builder.AddEndpointFilter(new EndpointAuthentication(roles));
        return builder;
    }
}
