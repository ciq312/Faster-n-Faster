namespace FasterNFaster.Api.UseCases.Auth;

// RefreshToken is null for guests (access-only, no rotation).
public record TokenPair(string AccessToken, string? RefreshToken);
