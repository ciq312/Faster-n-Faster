using FasterNFaster.Api.UseCases.Auth.Ports;

namespace FasterNFaster.Api.UseCases.Auth.RefreshToken;

public record RefreshTokenResult(TokenPair Tokens);
