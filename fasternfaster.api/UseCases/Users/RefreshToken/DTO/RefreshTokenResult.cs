using FasterNFaster.Api.UseCases.Auth.Tokens;

namespace FasterNFaster.Api.UseCases.Users.RefreshToken.DTO;

public record RefreshTokenResult(TokenPair Tokens);
