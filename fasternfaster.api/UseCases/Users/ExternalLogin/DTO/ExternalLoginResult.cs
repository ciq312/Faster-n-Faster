using FasterNFaster.Api.UseCases.Auth.Tokens;

namespace FasterNFaster.Api.UseCases.Users.ExternalLogin.DTO;

public record ExternalLoginResult(TokenPair Tokens);
