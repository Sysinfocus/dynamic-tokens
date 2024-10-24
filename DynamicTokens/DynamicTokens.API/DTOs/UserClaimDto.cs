namespace DynamicTokens.API.DTOs;

public record struct UserClaimDto(Guid Id, string Username, string Role);