using System.Text.Json;
using System.Text;

namespace DynamicTokens.API.Authentication;

public class EndpointAuthentication(string? roles = null) : IEndpointFilter
{
    private static readonly JsonSerializerOptions _jso = new() { PropertyNameCaseInsensitive = true };

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var token = context.HttpContext.Request.Headers.Authorization.ToString();
        if (!TokenService.ValidateClaimsToken(token)) return Results.Unauthorized();
        if (!IsInRole(token)) return Results.Forbid();
        return await next(context);
    }

    private bool IsInRole(string token)
    {
        if (roles is null) return true;
        var claims = token.Split('.')[0];
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(claims));
        var userClaim = JsonSerializer.Deserialize<UserClaim>(json, _jso);
        var allRoles = roles?.Split(',') ?? [];
        bool final = false;
        foreach (var item in allRoles)
        {
            if (item.Trim().Equals(userClaim.Role.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                final = true;
                break;
            }
        }
        return final;
    }
}


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
