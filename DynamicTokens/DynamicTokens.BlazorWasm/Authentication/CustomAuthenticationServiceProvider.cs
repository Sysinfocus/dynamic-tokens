﻿using DynamicTokens.BlazorWasm.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DynamicTokens.BlazorWasm.Authentication;
public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IJSRuntime _jSRuntime;
    private readonly NavigationManager _navigationManager;

    public CustomAuthenticationStateProvider(IJSRuntime jSRuntime, NavigationManager navigationManager)
    {
        _jSRuntime = jSRuntime;
        _navigationManager = navigationManager;
    }

    private readonly static JsonSerializerOptions _jso = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var tokens = await CheckToken();
        if (tokens is null) return new AuthenticationState(new ClaimsPrincipal());

        var userData = tokens.Split('.');
        var base64 = Encoding.UTF8.GetString(Convert.FromBase64String(userData[0]));
        var userClaims = JsonSerializer.Deserialize<UserClaim>(base64, _jso);
        var claimsIdentity = new ClaimsIdentity("dynamic-auth");
        claimsIdentity.AddClaim(new(ClaimTypes.Sid, userClaims.Id.ToString()));
        claimsIdentity.AddClaim(new(ClaimTypes.Name, userClaims.Username));
        claimsIdentity.AddClaim(new(ClaimTypes.Role, userClaims.Role));
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
        return new AuthenticationState(claimsPrincipal);
    }

    private async ValueTask<string?> CheckToken()
    {
        var token = await _jSRuntime.InvokeAsync<string?>("localStorage.getItem", "usertoken");
        if (token is null || string.IsNullOrEmpty(token)) return null;
        var userToken = JsonSerializer.Deserialize<UserTokens>(token, _jso);
        if (userToken is null) return null;

        userToken.Tokens.TryPeek(out string? tokenNow);
        if (string.IsNullOrEmpty(tokenNow)) return null;
        return $"{userToken.Claims}.{tokenNow}";
    }


    public async ValueTask<string?> GetToken()
    {
        var token = await _jSRuntime.InvokeAsync<string?>("localStorage.getItem", "usertoken");
        if (token is null || string.IsNullOrEmpty(token)) return null;
        var userToken = JsonSerializer.Deserialize<UserTokens>(token, _jso);
        if (userToken is null) return null;

        userToken.Tokens.TryDequeue(out string? tokenNow);

        if (string.IsNullOrEmpty(tokenNow))
        {
            userToken = await AttemptRefreshToken(userToken.Claims);
            if (userToken is null)
            {
                await _jSRuntime.InvokeVoidAsync("localStorage.removeItem", "usertoken");
                _navigationManager.NavigateTo(".", true);
                return null;
            }
            userToken.Tokens.TryDequeue(out string? tokenRefreshNow);
            await SaveToken(userToken);
            return $"{userToken.Claims}.{tokenRefreshNow}";
        }
        else
        {
            await SaveToken(userToken);
            return $"{userToken.Claims}.{tokenNow}";
        }
    }

    private static async Task<UserTokens?> AttemptRefreshToken(string claims)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", claims);
        var response = await client.PostAsJsonAsync<UserTokens?>($"{ApiService.API_URL}/user/refresh", null);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var userTokens = await response.Content.ReadFromJsonAsync<UserTokens>();
        return userTokens;
    }

    public async ValueTask SaveToken(UserTokens userTokens)
    {
        var json = JsonSerializer.Serialize(userTokens, _jso);
        await _jSRuntime.InvokeVoidAsync("localStorage.setItem", "usertoken", json);
    }

    public async ValueTask Logout()
        => await _jSRuntime.InvokeVoidAsync("localStorage.removeItem", "usertoken");
}
