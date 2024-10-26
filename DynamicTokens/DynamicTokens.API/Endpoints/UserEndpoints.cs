using DynamicTokens.API.Authentication;
using DynamicTokens.API.DTOs;

namespace DynamicTokens.API.Endpoints;

public class UserEndpoints(ITokenService tokenService) : IEndpoint
{
    public void Register(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/user").WithTags("Users");
        group.MapPost("/login", UserLogin);
        group.MapPost("/logout", UserLogout).ApplyEndpointAuthentication(tokenService);
        group.MapPost("/refresh", UserRefreshTokens);
    }

    private IResult UserLogin(LoginRequestDto request)
    {
        if (request.Username.Trim().Length >= 2 && request.Password.Trim().Length >= 4)
        {
            var userClaim = new UserClaimDto(Guid.NewGuid(), request.Username, request.Username == "admin" ? "Admin" : "User");
            var (claims, tokens) = tokenService.GetTokens(userClaim);
            return Results.Ok(new
            {
                Claims = claims,
                Tokens = tokens
            });
        }
        else
        {
            return Results.BadRequest("Invalid Username and/or Password.");
        }
    }

    private IResult UserLogout(HttpRequest request)
    {
        var claims = request.Headers.FirstOrDefault(h => h.Key == "Authorization").Value.ToString();
        if (claims is null) return Results.Unauthorized();
        var result = tokenService.RemoveToken(claims.Split('.')[0]);
        return result ? Results.Ok(true) : Results.Unauthorized();
    }
    
    private IResult UserRefreshTokens(HttpRequest request)
    {
        var auth = request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(auth)) return Results.Unauthorized();

        var (claimResult, tokens) = tokenService.RefreshTokens(auth);
        if (claimResult is null) return Results.Unauthorized();

        return Results.Ok(new
        {
            Claims = claimResult,
            Tokens = tokens
        });
    }
}
