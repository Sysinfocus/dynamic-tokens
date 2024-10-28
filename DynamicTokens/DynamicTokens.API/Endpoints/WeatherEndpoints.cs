using DynamicTokens.API.Authentication;
using DynamicTokens.API.DTOs;
using MongoDB.Driver;

namespace DynamicTokens.API.Endpoints;

internal class WeatherEndpoints(ITokenService tokenService, IMongoClient mongoClient) : IEndpoint
{
    string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

    public void Register(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/weather").WithTags("Weather");
        group.MapGet("/", GetWeatherForecast).ApplyEndpointAuthentication(tokenService, "user");
        group.MapGet("/admin", GetWeatherForecast).ApplyEndpointAuthentication(tokenService, "admin");
    }

    private async Task<WeatherForecastDto[]?> GetWeatherForecast()
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
            new WeatherForecastDto
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]
            ))
            .ToArray();

        var mongoDb = mongoClient.GetDatabase("weather");
        var collection = mongoDb.GetCollection<WeatherForecastDto>("forecasts");
        await collection.InsertManyAsync(forecast);

        return forecast;
    }
}
