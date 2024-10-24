
namespace DynamicTokens.API.Endpoints;

internal interface IEndpoint
{
    void Register(IEndpointRouteBuilder app);
}