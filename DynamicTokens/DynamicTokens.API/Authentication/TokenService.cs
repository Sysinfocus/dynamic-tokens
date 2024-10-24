using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text;

namespace DynamicTokens.API.Authentication;

public static class TokenService
{
    private readonly static JsonSerializerOptions _jso = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly static ConcurrentDictionary<string, Queue<string>> _userTokens = [];

    public static (string claims, IEnumerable<string> tokens) GetTokens(UserClaim userClaims, int tokenCount = 25)
    {
        var json = JsonSerializer.Serialize(userClaims, _jso);
        var claims = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        var tokens = new Queue<string>();
        for (int i = 0; i < tokenCount; i++)
        {
            tokens.Enqueue(Guid.NewGuid().ToString().Split('-')[^1]);
        }
        _userTokens[claims] = tokens;
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
        return (claims, tokens);
    }

    public static bool RemoveToken(string? claims)
    {
        if (claims is null || !_userTokens.ContainsKey(claims)) return false;
        _userTokens.Keys.Remove(claims);
        return true;
    }

    public static bool ValidateClaimsToken(string? key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        var values = key.Split('.');
        if (values.Length != 2) return false;
        _userTokens.TryGetValue(values[0], out Queue<string>? tokens);
        if (tokens is null) return false;
        tokens.TryDequeue(out string? dq);
        if (values[1].Equals(dq)) return true;
        else
        {
            _userTokens.Keys.Remove(values[0]);
            return false;
        }
    }
}