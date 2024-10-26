using DynamicTokens.BlazorWasm;
using DynamicTokens.BlazorWasm.Authentication;
using DynamicTokens.BlazorWasm.DTOs;
using DynamicTokens.BlazorWasm.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Net.Http.Json;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var client = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
#if !DEBUG
    var settings = await client.GetFromJsonAsync<SettingDto>("appsettings.json");
    ApiService.API_URL = settings.ApiUrl;
#endif

builder.Services.AddScoped(sp => client);
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddScoped<ApiService>();
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

await app.RunAsync();
