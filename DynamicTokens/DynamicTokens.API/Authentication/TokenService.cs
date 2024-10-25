using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text;
using DynamicTokens.API.DTOs;

namespace DynamicTokens.API.Authentication;

public class TokenService
{
    public static ILogger logger = default!;

    private readonly static JsonSerializerOptions _jso = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static ConcurrentDictionary<string, Queue<string>> _userTokens = [];

    public static (string claims, IEnumerable<string> tokens) GetTokens(UserClaimDto userClaims, int tokenCount = 25)
    {
        var json = JsonSerializer.Serialize(userClaims, _jso);
        var claims = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        var tokens = new Queue<string>();
        for (int i = 0; i < tokenCount; i++) tokens.Enqueue(Guid.NewGuid().ToString().Split('-')[^1]);
        _userTokens[claims] = tokens;
        logger.LogInformation($"{tokenCount} tokens generated for username: {userClaims.Username}.");
        return (claims, tokens);
    }

    public static (string? claims, IEnumerable<string>? tokens) RefreshTokens(string? claims, int tokenCount = 25)
    {
        if (claims is null || !_userTokens.ContainsKey(claims)) return (null, null);
        var tokens = new Queue<string>();
        for (int i = 0; i < tokenCount; i++)
        {
            tokens.Enqueue(Guid.NewGuid().ToString().Split('-')[^1]);
        }
        _userTokens[claims] = tokens;        
        var uc = JsonSerializer.Deserialize<UserClaimDto>(Convert.FromBase64String(claims), _jso);
        logger.LogInformation($"{tokenCount} refresh tokens generated for username: {uc.Username}.");
        return (claims, tokens);
    }

    public static bool RemoveToken(string? claims)
    {
        if (claims is null || !_userTokens.ContainsKey(claims)) return false;
        _userTokens.TryRemove(claims, out var _);
        var uc = JsonSerializer.Deserialize<UserClaimDto>(Convert.FromBase64String(claims), _jso);
        logger.LogInformation($"Token removed for username: {uc.Username}.");
        return true;
    }

    public static bool ValidateClaimsToken(string? key)
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
        _userTokens.TryGetValue(values[0], out Queue<string>? tokens);
        if (tokens is null)
        {
            var uc = JsonSerializer.Deserialize<UserClaimDto>(Convert.FromBase64String(values[0]), _jso);
            logger.LogError($"{nameof(ValidateClaimsToken)} failed as tokens are null for username: {uc.Username}.");
            return false;
        }
        tokens.TryDequeue(out string? dq);
        if (values[1].Equals(dq))
        {
            return true;
        }
        else
        {
            _userTokens.TryRemove(values[0], out var _);
            var uc = JsonSerializer.Deserialize<UserClaimDto>(Convert.FromBase64String(values[0]), _jso);
            logger.LogInformation($"Token removed for username: {uc.Username}.");
            return false;
        }
    }
}