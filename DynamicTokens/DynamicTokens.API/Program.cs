using DynamicTokens.API.Authentication;
using DynamicTokens.API.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddMongoDBClient("dynamic-tokens-mongo-db");
builder.AddRedisDistributedCache("dynamic-tokens-cache");

builder.Services.AddEndpointAuthenticationService();
builder.Services.AddMinimalAPIEndpoints();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseHttpsRedirection();

app.UseEndpointAuthentication();
app.UseMinimalAPIEndpoints();

app.Run();
