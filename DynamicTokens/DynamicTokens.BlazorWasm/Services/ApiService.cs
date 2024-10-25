using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DynamicTokens.BlazorWasm.Authentication;
using DynamicTokens.BlazorWasm.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace DynamicTokens.BlazorWasm.Services;
public class ApiService(
    AuthenticationStateProvider authState,
    HttpClient httpClient,
    NavigationManager navigationManager,
    IJSRuntime jSRuntime,
    ILogger<ApiService> logger)
{
    public static string API_URL = "https://localhost:7100";
    
    public async Task<UserClaimDto?> User()
    {
        var json = await jSRuntime.InvokeAsync<string?>("localStorage.getItem", "usertoken");
        if (string.IsNullOrEmpty(json)) return null;
        var userData = JsonSerializer.Deserialize<UserTokensDto>(json, _jso);
        if (userData is null) return null;
        var base64 = Encoding.UTF8.GetString(Convert.FromBase64String(userData.Claims));
        var userClaims = JsonSerializer.Deserialize<UserClaimDto?>(base64, _jso);
        logger.LogInformation($"Info access for user: {userClaims?.Username}.");
        return userClaims;
    }

    private readonly static JsonSerializerOptions _jso = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private async Task SetAuthentication()
    {
        httpClient.DefaultRequestHeaders.Remove("Authorization");
        var token = await ((CustomAuthenticationStateProvider)authState).GetToken();
        if (string.IsNullOrEmpty(token))
        {
            logger.LogWarning("No authorization header for this api call.");
            return;
        }
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);
    }

    public async Task<Result<TOutput>> ExecuteAsync<TOutput>(string url, HttpMethod method, object? input = null, bool redirectToLoginIfUnAuthorized = true, CancellationToken cancellationToken = default)
    {
        await SetAuthentication();
        var request = new HttpRequestMessage(method, url);
        if (input is not null)
        {
            var json = JsonSerializer.Serialize(input, _jso);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }
        try
        {
            var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized && redirectToLoginIfUnAuthorized)
            {
                logger.LogWarning($"Unauthorized access, hence redirecting to Login.");
                await Logout();
                return Result<TOutput>.Failure(response.StatusCode, null);
            }
            else if (!response.IsSuccessStatusCode)
            {
                var message = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning($"Api call returned code: {response.StatusCode} with message: {message}.");
                return Result<TOutput>.Failure(response.StatusCode, message);
            }
            else
            {
                var data = await response.Content.ReadFromJsonAsync<TOutput>(cancellationToken);
                return Result<TOutput>.Success(response.StatusCode, data);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning($"Api call resulted in an exception: {ex.Message}.");
            return Result<TOutput>.Failure(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    public async ValueTask Logout()
    {
        await SetAuthentication();
        await httpClient.PostAsync($"{API_URL}/user/logout", null);
        await jSRuntime.InvokeVoidAsync("localStorage.removeItem", "usertoken");
        navigationManager.NavigateTo("/", true);
    }

}