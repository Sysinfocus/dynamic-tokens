var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.DynamicTokens_API>("dynamic-tokens-api");
builder.AddProject<Projects.DynamicTokens_BlazorWasm>("dynamic-tokens-blazor-wasm")
    .WithReference(api);

builder.Build().Run();
