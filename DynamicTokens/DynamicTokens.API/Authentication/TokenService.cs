using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text;
using DynamicTokens.API.DTOs;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace DynamicTokens.API.Authentication;

public class TokenService(IMemoryCache cache, ILogger<TokenService> logger) : ITokenService
{
    private readonly static JsonSerializerOptions _jso = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public (string claims, IEnumerable<string> tokens) GetTokens(UserClaimDto userClaims, int tokenCount = 25)
    {
        var json = JsonSerializer.Serialize(userClaims, _jso);
        var claims = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        var tokens = new Queue<string>();
        for (int i = 0; i < tokenCount; i++) tokens.Enqueue(Guid.NewGuid().ToString().Split('-')[^1]);
        cache.Set(claims, Encoding.UTF8.GetBytes(string.Join(',', tokens)));
        logger.LogInformation($"{tokenCount} tokens generated for username: {userClaims.Username}.");
        return (claims, tokens);
    }

    public (string? claims, IEnumerable<string>? tokens) RefreshTokens(string? claims, int tokenCount = 25)
    {
        if (claims is null || cache.Get(claims) is null) return (null, null);

        var tokenBytes = (byte[])cache.Get(claims)!;
        var tokenArray = Encoding.UTF8.GetString(tokenBytes);
        var tokenData = tokenArray.Split(',');
        if (tokenData.Length != 1)
        {
            var ucm = JsonSerializer.Deserialize<UserClaimDto>(Convert.FromBase64String(claims), _jso);
            logger.LogWarning($"Refresh tokens claimed before its empty for username: {ucm.Username}.");
            return (null, null);
        }
        var tokens = new Queue<string>();
        for (int i = 0; i < tokenCount; i++)
        {
            tokens.Enqueue(Guid.NewGuid().ToString().Split('-')[^1]);
        }
        cache.Set(claims, Encoding.UTF8.GetBytes(string.Join(',', tokens)));
        var uc = JsonSerializer.Deserialize<UserClaimDto>(Convert.FromBase64String(claims), _jso);
        logger.LogInformation($"{tokenCount} refresh tokens generated for username: {uc.Username}.");
        return (claims, tokens);
    }

    public bool RemoveToken(string? claims)
    {
        if (claims is null || cache.Get(claims) is null) return false;
        cache.Remove(claims);
        var uc = JsonSerializer.Deserialize<UserClaimDto>(Convert.FromBase64String(claims), _jso);
        logger.LogInformation($"Token removed for username: {uc.Username}.");
        return true;
    }

    public bool ValidateClaimsToken(string? key)
    {
        if (string.IsNullOrEmpty(key))
        {
            logger.LogError($"{nameof(ValidateClaimsToken)} failed due to null/empty key.");
            return false;
        }
        var values = key.Split('.');
        if (values.Length != 2)
        {
            logger.LogError($"{nameof(ValidateClaimsToken)} failed due to invalid key: {key}");
            return false;
        }
        var tokenBytes = (byte[])cache.Get(values[0])!;
        if (tokenBytes is null)
        {
            logger.LogError($"{nameof(ValidateClaimsToken)} failed due to missing tokens for key: {values[0]}");
            return false;
        }
        var tokenArray = Encoding.UTF8.GetString(tokenBytes);
        var tokens = new Queue<string>();
        var tokenData = tokenArray.Split(',');
        foreach (var item in tokenData) tokens.Enqueue(item);
        if (tokens is null)
        {
            var uc = JsonSerializer.Deserialize<UserClaimDto>(Convert.FromBase64String(values[0]), _jso);
            logger.LogError($"{nameof(ValidateClaimsToken)} failed as tokens are null for username: {uc.Username}.");
            return false;
        }
        tokens.TryDequeue(out string? dq);
        if (values[1].Equals(dq))
        {
            cache.Set(values[0], Encoding.UTF8.GetBytes(string.Join(',', tokens)));
            return true;
        }
        else
        {
            cache.Remove(values[0]);
            var uc = JsonSerializer.Deserialize<UserClaimDto>(Convert.FromBase64String(values[0]), _jso);
            logger.LogInformation($"Token removed for username: {uc.Username}.");
            return false;
        }
    }
}