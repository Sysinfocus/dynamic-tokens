namespace DynamicTokens.BlazorWasm.Authentication;

public record UserTokens(string Claims, Queue<string> Tokens);