namespace FasterNFaster.Api.UseCases.Auth.Ports;

public record TokenPair(IssuedToken AccessToken, IssuedToken RefreshToken);
