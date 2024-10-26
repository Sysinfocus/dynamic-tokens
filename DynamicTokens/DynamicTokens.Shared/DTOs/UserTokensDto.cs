namespace DynamicTokens.Shared.DTOs;

public record UserTokensDto(string Claims, Queue<string> Tokens);