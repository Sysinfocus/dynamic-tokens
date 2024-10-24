using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DynamicTokens.BlazorWasm.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace DynamicTokens.BlazorWasm.Services;
public class ApiService
{
    public static string API_URL = "https://localhost:7100";
    private readonly CustomAuthenticationStateProvider _authState;
    private readonly HttpClient _httpClient;
    private readonly NavigationManager _navigationManager;
    private readonly IJSRuntime _jSRuntime;

    public ApiService(AuthenticationStateProvider authState, HttpClient httpClient, NavigationManager navigationManager, IJSRuntime jSRuntime)
    {
        _authState = (CustomAuthenticationStateProvider)authState;
        _httpClient = httpClient;
        _navigationManager = navigationManager;
        _jSRuntime = jSRuntime;
    }

    public async Task<UserClaim?> User()
    {
        var json = await _jSRuntime.InvokeAsync<string?>("localStorage.getItem", "usertoken");
        if (string.IsNullOrEmpty(json)) return null;
        var userData = JsonSerializer.Deserialize<UserTokens>(json, _jso);
        if (userData is null) return null;
        var base64 = Encoding.UTF8.GetString(Convert.FromBase64String(userData.Claims));
        var userClaims = JsonSerializer.Deserialize<UserClaim?>(base64, _jso);
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
        _httpClient.DefaultRequestHeaders.Remove("Authorization");
        var token = await _authState.GetToken();
        if (string.IsNullOrEmpty(token)) return;
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);
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
            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized && redirectToLoginIfUnAuthorized)
            {
                await Logout();
                return Result<TOutput>.Failure(response.StatusCode, null);
            }
            else if (!response.IsSuccessStatusCode)
            {
                var message = await response.Content.ReadAsStringAsync(cancellationToken);
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
            return Result<TOutput>.Failure(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    public async ValueTask Logout()
    {
        await SetAuthentication();
        await _httpClient.PostAsync($"{API_URL}/user/logout", null);
        await _jSRuntime.InvokeVoidAsync("localStorage.removeItem", "usertoken");
        _navigationManager.NavigateTo("/", true);
    }

}