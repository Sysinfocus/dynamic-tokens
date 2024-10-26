using DynamicTokens.API.DTOs;

namespace DynamicTokens.API.Authentication;
public interface ITokenService
{
    (string claims, IEnumerable<string> tokens) GetTokens(UserClaimDto userClaims, int tokenCount = 25);
    (string? claims, IEnumerable<string>? tokens) RefreshTokens(string? claims, int tokenCount = 25);
    bool RemoveToken(string? claims);
    bool ValidateClaimsToken(string? key);
}