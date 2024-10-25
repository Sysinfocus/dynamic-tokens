using DynamicTokens.API.Authentication;
using DynamicTokens.API.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddEndpointAuthenticationService();
builder.Services.AddMinimalAPIEndpoints();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseHttpsRedirection();

app.UseEndpointAuthentication();
app.UseMinimalAPIEndpoints();

app.Run();
