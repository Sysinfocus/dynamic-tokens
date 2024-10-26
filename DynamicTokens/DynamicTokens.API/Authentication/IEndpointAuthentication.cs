
namespace DynamicTokens.API.Authentication;

public interface IEndpointAuthentication
{
    ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next);
}