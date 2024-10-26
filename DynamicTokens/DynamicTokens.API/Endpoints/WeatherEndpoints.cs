using DynamicTokens.API.Authentication;
using DynamicTokens.API.DTOs;

namespace DynamicTokens.API.Endpoints;

internal class WeatherEndpoints : IEndpoint
{
    string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

    public void Register(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/weather").WithTags("Weather");
        group.MapGet("/", GetWeatherForecast).ApplyEndpointAuthentication("user");
        group.MapGet("/admin", GetWeatherForecast).ApplyEndpointAuthentication("admin");
    }

    private WeatherForecastDto[]? GetWeatherForecast()
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
            new WeatherForecastDto
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]
            ))
            .ToArray();
        return forecast;
    }
}
