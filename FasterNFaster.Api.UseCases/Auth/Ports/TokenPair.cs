namespace FasterNFaster.Api.UseCases.Auth.Ports;

// RefreshToken is null for guests (access-only, no rotation).
public record TokenPair(IssuedToken AccessToken, IssuedToken? RefreshToken);
