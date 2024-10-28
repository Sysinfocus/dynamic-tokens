namespace DynamicTokens.API.Authentication;

public static class EndpointAuthenticationExtensions
{
    public static void AddEndpointAuthenticationService(this IServiceCollection service)
    {
        service.AddAuthentication().AddBearerToken();
        //service.AddMemoryCache();
        service.AddSingleton<ITokenService, TokenService>();
        service.AddSingleton<IEndpointAuthentication, EndpointAuthentication>();
        service.AddCors();
    }

    public static void UseEndpointAuthentication(this WebApplication app)
    {
        app.UseCors(o => o
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin()
        );
    }

    public static RouteHandlerBuilder ApplyEndpointAuthentication(
        this RouteHandlerBuilder builder,
        ITokenService tokenService,
        string? roles = null)
    {                
        builder.AddEndpointFilter(new EndpointAuthentication(tokenService, roles));
        return builder;
    }
}
