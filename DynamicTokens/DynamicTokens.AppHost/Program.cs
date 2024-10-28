var builder = DistributedApplication.CreateBuilder(args);

var mongo = builder.AddMongoDB("dynamic-tokens-mongo", 27017);
var mongoDb = mongo.AddDatabase("dynamic-tokens-mongo-db");

var cache = builder.AddRedis("dynamic-tokens-cache", 6379);

var api = builder.AddProject<Projects.DynamicTokens_API>("dynamic-tokens-api")
    .WithReference(cache)
    .WithReference(mongoDb);

builder.AddProject<Projects.DynamicTokens_BlazorWasm>("dynamic-tokens-blazor-wasm")
    .WithReference(api);

builder.AddProject<Projects.DynamicTokens_BlazorSSR>("dynamic-tokens-blazor-ssr")
    .WithReference(api);

builder.Build().Run();
