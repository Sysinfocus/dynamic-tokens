namespace DynamicTokens.Shared.DTOs;

public sealed record WeatherForecastDto(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}