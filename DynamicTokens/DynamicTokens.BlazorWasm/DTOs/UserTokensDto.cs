namespace DynamicTokens.BlazorWasm.DTOs;

public record UserTokensDto(string Claims, Queue<string> Tokens);