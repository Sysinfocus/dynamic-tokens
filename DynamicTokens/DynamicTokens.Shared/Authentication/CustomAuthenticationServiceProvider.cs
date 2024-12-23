﻿using DynamicTokens.Shared.DTOs;
using DynamicTokens.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DynamicTokens.Shared.Authentication;
public class CustomAuthenticationStateProvider(
    ILogger<CustomAuthenticationStateProvider> logger,
    IJSRuntime jSRuntime,
    NavigationManager navigationManager) : AuthenticationStateProvider
{
    private readonly static JsonSerializerOptions _jso = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var tokens = await CheckToken();
        if (tokens is null)
        {
            logger.LogError($"Token is null.");
            return new AuthenticationState(new ClaimsPrincipal());
        }

        var userData = tokens.Split('.');
        var base64 = Encoding.UTF8.GetString(Convert.FromBase64String(userData[0]));
        var userClaims = JsonSerializer.Deserialize<UserClaimDto>(base64, _jso);
        if (userClaims is null)
        {
            logger.LogError($"User claims are null.");
            return new AuthenticationState(new ClaimsPrincipal());
        }

        var claimsIdentity = new ClaimsIdentity("dynamic-auth");
        claimsIdentity.AddClaim(new(ClaimTypes.Sid, userClaims.Id.ToString()));
        claimsIdentity.AddClaim(new(ClaimTypes.Name, userClaims.Username));
        claimsIdentity.AddClaim(new(ClaimTypes.Role, userClaims.Role));
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        logger.LogInformation($"Username: {userClaims.Username} is now authenticated.");
        return new AuthenticationState(claimsPrincipal);
    }

    private async ValueTask<string?> CheckToken()
    {
        var token = await jSRuntime.InvokeAsync<string?>("localStorage.getItem", "usertoken");
        if (token is null || string.IsNullOrEmpty(token)) return null;
        var userToken = JsonSerializer.Deserialize<UserTokensDto>(token, _jso);
        if (userToken is null) return null;

        userToken.Tokens.TryPeek(out string? tokenNow);
        if (string.IsNullOrEmpty(tokenNow)) return null;
        return $"{userToken.Claims}.{tokenNow}";
    }


    public async ValueTask<string?> GetToken()
    {
        var token = await jSRuntime.InvokeAsync<string?>("localStorage.getItem", "usertoken");
        if (token is null || string.IsNullOrEmpty(token)) return null;
        var userToken = JsonSerializer.Deserialize<UserTokensDto>(token, _jso);
        if (userToken is null) return null;

        userToken.Tokens.TryDequeue(out string? tokenNow);

        if (string.IsNullOrEmpty(tokenNow))
        {
            userToken = await AttemptRefreshToken(userToken.Claims);
            if (userToken is null)
            {
                await jSRuntime.InvokeVoidAsync("localStorage.removeItem", "usertoken");
                navigationManager.NavigateTo(".", true);
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

    private async Task<UserTokensDto?> AttemptRefreshToken(string claims)
    {
        var userClaims = JsonSerializer.Deserialize<UserClaimDto>(Convert.FromBase64String(claims), _jso);
        var client = new HttpClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", claims);
        var response = await client.PostAsJsonAsync<UserTokensDto?>($"{ApiService.API_URL}/api/user/refresh", null);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError($"Refresh token attempt failed for user: {userClaims?.Username}.");
            return null;
        }
        var userTokens = await response.Content.ReadFromJsonAsync<UserTokensDto>();
        logger.LogInformation($"Refresh token generated for user: {userClaims?.Username}.");
        return userTokens;
    }

    public async ValueTask SaveToken(UserTokensDto userTokens)
    {
        var json = JsonSerializer.Serialize(userTokens, _jso);
        await jSRuntime.InvokeVoidAsync("localStorage.setItem", "usertoken", json);
    }

    public async ValueTask Logout()
        => await jSRuntime.InvokeVoidAsync("localStorage.removeItem", "usertoken");
}
