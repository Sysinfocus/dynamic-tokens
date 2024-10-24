﻿namespace DynamicTokens.API.Authentication;

public static class EndpointAuthenticationExtensions
{
    public static void AddEndpointAuthenticationService(this IServiceCollection service)
    {
        service.AddAuthentication().AddBearerToken();
        service.AddCors();
        service.AddSingleton<EndpointAuthentication>();
    }

    public static void UseEndpointAuthentication(this WebApplication app)
    {
        app.UseCors(o => o.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());

    }

    public static RouteHandlerBuilder ApplyEndpointAuthentication(this RouteHandlerBuilder builder, string? roles = null)
    {
        builder.AddEndpointFilter(new EndpointAuthentication(roles));
        return builder;
    }
}
