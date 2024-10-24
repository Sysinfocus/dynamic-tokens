namespace DynamicTokens.API.Authentication;

public record struct UserClaim(Guid Id, string Username, string Role);