namespace DynamicTokens.Shared.Services;

using System.Net;

public class Result<T>
{
    public HttpStatusCode StatusCode { get; init; }
    public T? Value { get; init; }
    public string? Message { get; set; }
    public object? Errors { get; init; }
    public bool IsSuccess { get; init; }

    public static Result<T> Success(HttpStatusCode statusCode, T? value)
    {
        return new Result<T>
        {
            StatusCode = statusCode,
            Value = value,
            IsSuccess = true
        };
    }
    public static Result<T> Failure(HttpStatusCode statusCode, string? message)
    {
        return new Result<T>
        {
            StatusCode = statusCode,
            Message = string.IsNullOrEmpty(message) ? $"Failed with status code: {statusCode}" : message,
            IsSuccess = false
        };
    }
}